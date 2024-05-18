namespace InteractiveCodeExecution.ExecutorEntities
{
    public class ExecutionResult
    {
        public ExecutorCommand.ExecutorStage Stage { get; set; }
        public long ReturnCode { get; set; }
    }
}