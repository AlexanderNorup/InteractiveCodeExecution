using InteractiveCodeExecution.Services;
using Microsoft.Extensions.Options;

namespace InteractiveCodeExecution
{
    /// <summary>
    /// Just a wrapper for a SemaphoreSlim, supposed to be registered as Singleton by ASP.NETCore
    /// </summary>
    public class RequestThrottler : SemaphoreSlim
    {
        public RequestThrottler(IOptions<DockerConfiguration> options, ILogger<RequestThrottler> logger)
            : this(options.Value.MaxConcurrentExecutions, options.Value.MaxConcurrentExecutions)
        {
            logger.LogInformation("Request throttler initialized with {MaxConcurrentExecutions} max concurrent executions",
                options.Value.MaxConcurrentExecutions);
        }

        public RequestThrottler(int initialCount) : base(initialCount)
        {
        }

        public RequestThrottler(int initialCount, int maxCount) : base(initialCount, maxCount)
        {
        }
    }
}
