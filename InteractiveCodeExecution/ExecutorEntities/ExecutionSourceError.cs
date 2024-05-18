using MessagePack;

namespace InteractiveCodeExecution.ExecutorEntities
{
    [MessagePackObject]
    public class ExecutionSourceError
    {
        [Key("AffectedFile")]
        public string AffectedFile { get; set; }
        [Key("Column")]
        public int? Column { get; set; }
        [Key("Line")]
        public int? Line { get; set; }
        [Key("Type")]
        public string Type { get; set; }
        [Key("ErrorCode")]
        public string ErrorCode { get; set; }
        [Key("ErrorMessage")]
        public string ErrorMessage { get; set; }
    }
}
