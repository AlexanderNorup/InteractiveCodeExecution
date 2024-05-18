using MessagePack;

namespace InteractiveCodeExecution.ExecutorEntities
{
    [MessagePackObject]
    public class ExecutorFile
    {
        [Key("Filepath")]
        public string Filepath { get; set; }
        [Key("Content")]
        public string Content { get; set; }
        [Key("ContentType")]
        public ExecutorFileType ContentType { get; set; }
    }
}