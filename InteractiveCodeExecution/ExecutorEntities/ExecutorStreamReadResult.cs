

namespace InteractiveCodeExecution.ExecutorEntities
{
    public struct ExecutorStreamReadResult
    {
        public int Count { get; set; }
        public ExecutorStreamType Target { get; set; }
        public bool EndOfStream => Count == 0;
    }

    public enum ExecutorStreamType
    {
        StdOut, StdErr, StdIn
    }
}