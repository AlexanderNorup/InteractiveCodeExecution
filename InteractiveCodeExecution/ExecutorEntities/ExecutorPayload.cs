
namespace InteractiveCodeExecution.ExecutorEntities
{
    public class ExecutorPayload
    {
        public string PayloadType { get; set; }
        public List<ExecutorFile> Files { get; set; }

        public string? BuildCmd { get; set; }
        public string ExecCmd { get; set; }
    }
}
