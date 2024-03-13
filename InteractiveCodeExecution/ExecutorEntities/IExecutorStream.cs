namespace InteractiveCodeExecution.ExecutorEntities
{
    public interface IExecutorStream
    {
        public Task<ExecutorStreamReadResult> ReadOutputAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
        public Task<ExecutionResult> GetExecutionResultAsync();
    }
}
