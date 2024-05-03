using MessagePack;

namespace InteractiveCodeExecution.ExecutorEntities
{
    [MessagePackObject]
    public class ExecutorAssignment
    {
        [Key("Id")]
        public string? AssignmentId { get; set; }
        [Key("Name")]
        public string? AssignmentName { get; set; }
        [Key("Image")]
        public string? Image { get; set; }
        [Key("Commands")]
        public IList<ExecutorCommand> Commands { get; set; } = new List<ExecutorCommand>();
        [Key("ExecutorConfig")]
        public ExecutorConfig? ExecutorConfig { get; set; }
        [Key("InitialPayload")]
        public List<ExecutorFile>? InitialPayload { get; set; }
    }
}
