namespace InteractiveCodeExecution.ExecutorEntities
{
    public class ExecutorHandle
    {
        public ExecutorContainer Container { get; set; }
        public bool HasBuildSteps { get; set; }

        public IList<(bool runInBackground, Func<Task<IExecutorStream?>> stream)> ExecutorStreams { get; set; } = new List<(bool, Func<Task<IExecutorStream?>>)>();
    }
}
