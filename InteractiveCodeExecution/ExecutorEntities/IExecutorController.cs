
namespace InteractiveCodeExecution.ExecutorEntities
{
    public interface IExecutorController
    {
        Task<ExecutorHandle> GetExecutorHandle(ExecutorPayload payload, ExecutorConfig config, CancellationToken cancellationToken = default);
        Task ReleaseHandle(ExecutorHandle payload);
    }
}
