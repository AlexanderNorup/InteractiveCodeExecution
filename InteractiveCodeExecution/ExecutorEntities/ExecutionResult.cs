namespace InteractiveCodeExecution.ExecutorEntities
{
    public class ExecutionResult
    {
        public ExecutionStage Stage { get; set; }
        public long ReturnCode { get; set; }

        public enum ExecutionStage
        {
            Build, Run
        }
    }
}