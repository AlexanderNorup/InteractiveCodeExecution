namespace InteractiveCodeExecution.ExecutorEntities
{
    public interface IExecutorAssignmentSubmissionHandler
    {
        public Task SubmitAssignmentAsync(ExecutorPayload payload, string userId, CancellationToken cancellationToken = default);
        public Task<IEnumerable<string>> GetAllSubmissionsForAssignmentAsync(string assignmentId, CancellationToken cancellationToken = default);
        public FileStream GetSubmissionTarBall(string assignmentId, string userId, CancellationToken cancellationToken = default);
    }
}
