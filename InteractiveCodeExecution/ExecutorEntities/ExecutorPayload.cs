
using MessagePack;

namespace InteractiveCodeExecution.ExecutorEntities
{
    [MessagePackObject]
    public class ExecutorPayload
    {
        [Key("PayloadType")]
        public string PayloadType { get; set; }
        [Key("Files")]
        public List<ExecutorFile> Files { get; set; }
        [Key("BackgroundCmd")]
        public string? BackgroundCmd { get; set; }
        [Key("BuildCmd")]
        public string? BuildCmd { get; set; }
        [Key("ExecCmd")]
        public string ExecCmd { get; set; }
    }
}
