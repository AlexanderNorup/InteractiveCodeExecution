using MessagePack;

namespace InteractiveCodeExecution.ExecutorEntities
{
    [MessagePackObject]
    public class ExecutorCommand
    {
        [Key("Command")]
        public string? Command { get; set; }
        [Key("WaitForExit")]
        public bool WaitForExit { get; set; } = true;
        [Key("ExecutorStage")]
        public ExecutorStage Stage { get; set; }

        public enum ExecutorStage
        {
            Build = 0,
            Exec = 1,
        }
    }
}
