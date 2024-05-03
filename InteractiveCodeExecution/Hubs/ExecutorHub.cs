using InteractiveCodeExecution.ExecutorEntities;
using InteractiveCodeExecution.SourceParsers;
using MessagePack;
using Microsoft.AspNetCore.SignalR;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace InteractiveCodeExecution.Hubs
{
    public class ExecutorHub : Hub
    {
        private IExecutorController _executor;
        private IExecutorAssignmentProvider _assignmentProvider;
        private RequestThrottler _throttler;

        private static ConcurrentDictionary<string, bool> s_userIsExecutingMap = new ConcurrentDictionary<string, bool>();

        // This variable is temporary. Is should be tied with Assignments when they're implemented.
        public static readonly ExecutorConfig DefaultConfig = new ExecutorConfig()
        {
            //Timeout = TimeSpan.FromMinutes(1),
            MaxMemoryBytes = 1024 * 1024 * 512L, // 512 MB ram
            //MaxVCpus = .5,
            MaxContainerSizeInBytes = 1024 * 1024 * 1,
            MaxPayloadSizeInBytes = 2000,
        };

        public ExecutorHub(IExecutorController executor, IExecutorAssignmentProvider assignmentProvider, RequestThrottler requestThrottler)
        {
            _executor = executor;
            _assignmentProvider = assignmentProvider;
            _throttler = requestThrottler;
        }

        public async IAsyncEnumerable<LogMessage> ExecutePayloadByStream(ExecutorPayload payload, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            string userId = Context.ConnectionId; // TODO: Should be replaced with a user-auth id
            yield return new($"[TEMPORARY] Your user-id is: {userId}", "debug");
            if (s_userIsExecutingMap.GetOrAdd(userId, false))
            {
                yield return new("You can only run a single concurrent execution per user", "error");
                yield break;
            }

            if (_throttler.CurrentCount <= 0)
            {
                yield return new("Waiting for an available container...", "debug");
            }
            await _throttler.WaitAsync(cancellationToken);
            try
            {
                if (s_userIsExecutingMap[userId])
                {
                    yield return new("You can only run a single concurrent execution per user", "error");
                    yield break;
                }

                s_userIsExecutingMap[userId] = true;
                await foreach (var status in StartExecutionAsync(payload, userId, cancellationToken))
                {
                    yield return status;
                }
            }
            finally
            {
                s_userIsExecutingMap[userId] = false;
                _throttler.Release();
            }
        }

        private async IAsyncEnumerable<LogMessage> StartExecutionAsync(ExecutorPayload payload, string userId, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Get assignment
            if (string.IsNullOrEmpty(payload.AssignmentId)
                || !_assignmentProvider.TryGetAssignment(payload.AssignmentId, out var assignment)
                || assignment is null)
            {
                yield return new("Could not associate request with an assignment", "error");
                yield break;
            }

            var config = assignment.ExecutorConfig ?? DefaultConfig;

            yield return new("Starting container...", "debug");
            ExecutorHandle? handle = null;
            Exception? handleException = null;
            try
            {
                handle = await _executor.GetExecutorHandle(payload, assignment, config, userId, Context.ConnectionAborted);
            }
            catch (Exception ex)
            {
                // Using this jank because you're not allowed to 'yield return' in catch blocks. 
                handleException = ex;
            }

            if (handle is null)
            {
                if (handleException is ExecutorPayloadTooBigException)
                {
                    yield return new(handleException.Message ?? "Failed to start a container", "error");
                }
                else if (handleException is not null)
                {
                    yield return new("Failed to start a container. Contact a system-admin", "error");
                    throw handleException; // We do it this way, because we don't want to expose all errors to users
                }
                yield break;
            }

            if (!handle.ExecutorStreams.Any())
            {
                yield return new("There are no commands to run.", "error");
                yield break;
            }

            yield return new("Starting execution!", "debug");

            var timeoutCancellationToken = new CancellationTokenSource();
            if (config.Timeout > TimeSpan.Zero)
            {
                timeoutCancellationToken.CancelAfter(config.Timeout.Value);
            }

            var timeoutCt = timeoutCancellationToken.Token;
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCt, cancellationToken);

            var linkedCancellationToken = linkedCts.Token;

            const int BufferSize = 4096;
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

            ISourceErrorParser sourceErrorParser = new CSharpSourceErrorParser(); //TODO: Resolve this and allow for others
            var sourceErrors = new List<ExecutionSourceError>();

            int completedStreamsCount = 0;
            var streams = handle.ExecutorStreams.GetEnumerator();
            try
            {
                streams.MoveNext();
                var streamToRun = await GetNextExecutorStream(streams).ConfigureAwait(false);
                if (streamToRun is null)
                {
                    yield break;
                }

                while (!linkedCancellationToken.IsCancellationRequested)
                {
                    ExecutorStreamReadResult result;
                    try
                    {
                        result = await streamToRun.ReadOutputAsync(buffer, 0, buffer.Length, linkedCancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // The reason we have a try-catch within a try-finally block is that 
                        // you cannot use "yield return" statements in a try-block with catch-statements. 
                        // Therefore we needs this janky workaround
                        break;
                    }
                    if (result.EndOfStream)
                    {
                        var executionResult = await streamToRun.GetExecutionResultAsync().ConfigureAwait(false);
                        completedStreamsCount++;
                        yield return new($"Execution stage {completedStreamsCount} ({executionResult.Stage}) exited with code: {executionResult.ReturnCode}", "debug");

                        if (executionResult.ReturnCode != 0)
                        {
                            break;
                        }

                        if (!streams.MoveNext())
                        {
                            // This was the last stream. Return and close the container
                            break;
                        }

                        streamToRun = await GetNextExecutorStream(streams).ConfigureAwait(false);
                        if (streamToRun is null)
                        {
                            break;
                        }
                        continue;
                    }

                    var outStr = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);

                    if (sourceErrorParser is not null)
                    {
                        foreach (var line in outStr.Split("\n"))
                        {
                            if (sourceErrorParser.TryParseLine(line, out var err) && err is { })
                            {
                                sourceErrors.Add(err);
                            }
                        }
                    }


                    yield return new($"{outStr}", result.Target == ExecutorStreamType.StdOut ? "information" : "error");
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                await _executor.ReleaseHandle(handle).ConfigureAwait(false);
            }
            if (timeoutCt.IsCancellationRequested)
            {
                yield return new($"Execution was aborted because it went on for too long. Maximum allowed time is {config.Timeout}", "error");
            }

            if (sourceErrors.Any())
            {
                await Clients.Caller.SendAsync("SourceErrors", sourceErrors, Context.ConnectionAborted).ConfigureAwait(false);
            }
        }

        private async static Task<IExecutorStream?> GetNextExecutorStream(IEnumerator<(bool runInBackground, Func<Task<IExecutorStream?>> stream)> enumerator)
        {
            IExecutorStream? nextWaitingStream = null;
            do
            {
                var currentStream = enumerator.Current;
                if (currentStream.runInBackground)
                {
                    _ = enumerator.Current.stream.Invoke();
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }
                    continue;
                }

                nextWaitingStream = await currentStream.stream.Invoke().ConfigureAwait(false);
            } while (nextWaitingStream is null);

            return nextWaitingStream;
        }

        [MessagePackObject]
        public struct LogMessage
        {
            public LogMessage(string message, string severity = "information")
            {
                Message = message;
                Severity = severity;
            }

            [Key("Message")]
            public string Message { get; set; }
            [Key("Severity")]
            public string Severity { get; set; }
        }
    }
}
