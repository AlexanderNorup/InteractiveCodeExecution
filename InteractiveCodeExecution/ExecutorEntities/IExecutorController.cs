
namespace InteractiveCodeExecution.ExecutorEntities
{
    public interface IExecutorController
    {
        Task<ExecutorHandle> GetExecutorHandle(ExecutorPayload payload, CancellationToken cancellationToken = default);
        Task ReleaseHandle(ExecutorHandle payload);
    }
}
