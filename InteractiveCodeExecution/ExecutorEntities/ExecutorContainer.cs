namespace InteractiveCodeExecution.ExecutorEntities
{
    public class ExecutorContainer
    {
        public string Id { get; set; }
        public string ContainerPath { get; set; }
        public string ContainerOwner { get; set; }
        public int? ContainerStreamPort { get; set; }
    }
}
