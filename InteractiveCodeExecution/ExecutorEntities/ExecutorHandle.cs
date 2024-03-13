namespace InteractiveCodeExecution.ExecutorEntities
{
    public class ExecutorHandle
    {
        public ExecutorContainer Container { get; set; }
        public bool ShouldBuild { get; set; }
        public Func<Task<IExecutorStream>> BuildStream { get; set; }
        public Func<Task<IExecutorStream>> ExecutorStream { get; set; }
    }
}
