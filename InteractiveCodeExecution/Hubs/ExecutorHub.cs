using InteractiveCodeExecution.ExecutorEntities;
using Microsoft.AspNetCore.SignalR;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace InteractiveCodeExecution.Hubs
{
    public class ExecutorHub : Hub
    {
        private IExecutorController _executor;
        public ExecutorHub(IExecutorController executor)
        {
            _executor = executor;
        }

        public async IAsyncEnumerable<LogMessage> ExecutePayloadByStream(ExecutorPayload payload, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new("Starting container...", "debug");
            var handle = await _executor.GetExecutorHandle(payload, Context.ConnectionAborted);

            yield return new("Starting execution!", "debug");

            const int BufferSize = 4096;
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            bool hasBeenBuilt = !handle.ShouldBuild;
            try
            {
                IExecutorStream streamToRun = hasBeenBuilt ? await handle.ExecutorStream() : await handle.BuildStream();

                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await streamToRun.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
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
        }

        public struct LogMessage
        {
            public LogMessage(string message, string severity = "information")
            {
                Message = message;
                Severity = severity;
            }

            public string Message { get; set; }
            public string Severity { get; set; }
        }
    }
}
