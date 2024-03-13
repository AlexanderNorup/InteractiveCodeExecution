using Docker.DotNet;
using Docker.DotNet.Models;
using InteractiveCodeExecution.ExecutorEntities;

namespace InteractiveCodeExecution.Services
{
    public class DockerStream : IExecutorStream
    {
        public MultiplexedStream Stream { get; }
        private ExecutionResult.ExecutionStage m_stage;
        private Func<Task<ContainerExecInspectResponse>> m_containerResultResolver;

        public DockerStream(MultiplexedStream stream, ExecutionResult.ExecutionStage executionStage, Func<Task<ContainerExecInspectResponse>> containerResultResolver)
        {
            Stream = stream;
            m_stage = executionStage;
            m_containerResultResolver = containerResultResolver;
        }

        public async Task<ExecutorStreamReadResult> ReadOutputAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            var result = await Stream.ReadOutputAsync(buffer, offset, buffer.Length, cancellationToken).ConfigureAwait(false);
            return new()
            {
                Count = result.Count,
                Target = AsExecutorStreamType(result.Target)
            };
        }

        private static ExecutorStreamType AsExecutorStreamType(MultiplexedStream.TargetStream targetStream) => targetStream switch
        {
            MultiplexedStream.TargetStream.StandardOut => ExecutorStreamType.StdOut,
            MultiplexedStream.TargetStream.StandardIn => ExecutorStreamType.StdIn,
            MultiplexedStream.TargetStream.StandardError => ExecutorStreamType.StdErr,
            _ => throw new ArgumentException($"Unknown {nameof(MultiplexedStream.TargetStream)} type: {targetStream}"),
        };

        public async Task<ExecutionResult> GetExecutionResultAsync()
        {
            var result = await m_containerResultResolver();
            return new()
            {
                Stage = m_stage,
                ReturnCode = result.ExitCode
            };
        }
    }
}
