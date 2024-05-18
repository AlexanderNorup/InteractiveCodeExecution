namespace InteractiveCodeExecution.ExecutorEntities
{
    public interface IExecutorAssignmentProvider
    {
        IEnumerable<ExecutorAssignment> GetAllAssignments();

        bool TryGetAssignment(string assignmentId, out ExecutorAssignment? assignment);
    }
}
