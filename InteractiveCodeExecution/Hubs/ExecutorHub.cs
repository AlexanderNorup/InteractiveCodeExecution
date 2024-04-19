using InteractiveCodeExecution.ExecutorEntities;
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
        private RequestThrottler _throttler;

        private static ConcurrentDictionary<string, bool> s_userIsExecutingMap = new ConcurrentDictionary<string, bool>();

        // This variable is temporary. Is should be tied with Assignments when they're implemented.
        private static readonly ExecutorConfig m_tempConfig = new ExecutorConfig()
        {
            Timeout = TimeSpan.FromMinutes(1),
            MaxMemoryBytes = 1024 * 1024 * 512L, // 512 MB ram
            //MaxVCpus = .5,
            MaxContainerSizeInBytes = 1024 * 1024 * 1,
            MaxPayloadSizeInBytes = 2000,
            EnvironmentVariables = new List<string>()
            {
                "RESOLUTION=854x480",
                "DISPLAY=:1.0"
            },
            HasVncServer = false
        };

        public ExecutorHub(IExecutorController executor, RequestThrottler requestThrottler)
        {
            _executor = executor;
            _throttler = requestThrottler;
        }

        public async IAsyncEnumerable<LogMessage> ExecutePayloadByStream(ExecutorPayload payload, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            //Temp until we have a tag to see VNC images (this breaks concurrency)
            m_tempConfig.HasVncServer = payload.PayloadType == "vncTest";

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
            yield return new("Starting container...", "debug");
            ExecutorHandle? handle = null;
            ExecutorPayloadTooBigException? payloadTooBigException = null;
            try
            {
                handle = await _executor.GetExecutorHandle(payload, m_tempConfig, userId, Context.ConnectionAborted);
            }
            catch (ExecutorPayloadTooBigException ex)
            {
                // Using this jank because you're not allowed to 'yield return' in catch blocks. 
                payloadTooBigException = ex;
            }

            if (handle is null)
            {
                yield return new(payloadTooBigException?.Message ?? "Failed to start a container", "error");
                yield break;
            }

            yield return new("Starting execution!", "debug");

            var timeoutCancellationToken = new CancellationTokenSource();
            if (m_tempConfig.Timeout > TimeSpan.Zero)
            {
                timeoutCancellationToken.CancelAfter(m_tempConfig.Timeout.Value);
            }

            var timeoutCt = timeoutCancellationToken.Token;
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCt, cancellationToken);

            var linkedCancellationToken = linkedCts.Token;

            const int BufferSize = 4096;
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            bool hasBeenBuilt = !handle.ShouldBuild;
            try
            {
                if (handle.BackgroundStream is { } bgStream)
                {
                    await bgStream(); // Fire and forget
                }

                IExecutorStream streamToRun = hasBeenBuilt ? await handle.ExecutorStream() : await handle.BuildStream();

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
                        var executionResult = await streamToRun.GetExecutionResultAsync();

                        yield return new($"Execution stage: {executionResult.Stage} exited with code: {executionResult.ReturnCode}", "debug");

                        if (hasBeenBuilt || executionResult.ReturnCode != 0)
                        {
                            break;
                        }

                        hasBeenBuilt = true;
                        streamToRun = await handle.ExecutorStream();
                        yield return new("Successfully built!", "debug");
                        continue;
                    }

                    var outStr = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
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
                yield return new($"Execution was aborted because it went on for too long. Maximum allowed time is {m_tempConfig.Timeout}", "error");
            }
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
