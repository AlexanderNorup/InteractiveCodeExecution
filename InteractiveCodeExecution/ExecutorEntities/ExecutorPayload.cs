
using MessagePack;

namespace InteractiveCodeExecution.ExecutorEntities
{
    [MessagePackObject]
    public class ExecutorPayload
    {
        [Key("AssignmentId")]
        public string? AssignmentId { get; set; }

        [Key("BuildOnly")]
        public bool BuildOnly { get; set; }

        [Key("Files")]
        public List<ExecutorFile>? Files { get; set; }
    }
}
