﻿
namespace InteractiveCodeExecution.ExecutorEntities
{
    public interface IExecutorController
    {
        Task<IList<ExecutorContainer>> GetAllManagedContainersAsync(CancellationToken cancellationToken = default);
        Task<ExecutorHandle> GetExecutorHandle(ExecutorPayload payload, ExecutorAssignment assignment, ExecutorConfig config, string userId, CancellationToken cancellationToken = default);
        Task ReleaseHandle(ExecutorHandle payload);
    }
}
