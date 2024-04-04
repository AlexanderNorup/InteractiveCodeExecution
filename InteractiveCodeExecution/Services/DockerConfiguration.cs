using System.ComponentModel.DataAnnotations;

namespace InteractiveCodeExecution.Services
{
    public class DockerConfiguration
    {
        [Required]
        public Dictionary<string, string> PayloadImageTypeMapping { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int MaxConcurrentExecutions { get; set; }
    }
}
