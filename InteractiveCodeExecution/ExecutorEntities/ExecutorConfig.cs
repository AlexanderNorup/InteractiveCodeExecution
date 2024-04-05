namespace InteractiveCodeExecution.ExecutorEntities
{
    public class ExecutorConfig
    {
        /// <summary>
        /// Sets a maximum runtime for the execution. This includes both build and exec time.<br/>
        /// Set to <see langword="null"/> in order to disable timeout.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Sets a maximum ram allocation of this executor.<br/>
        /// Set to <see langword="null"/> in order to disable max memory (not recommended).
        /// </summary>
        public long? MaxMemoryBytes { get; set; }

        /// <summary>
        /// Sets a maximum number of VCpu's the execution is allowed to use.<br/>
        /// 1.0 means 1 full CPU core can be used. 1.5 means it is allowed to use one full CPU core and half of another core. <br/>
        /// Set to <see langword="null"/> in order to disable max VCPUs' (not recommended).
        /// </summary>
        public double? MaxVCpus { get; set; }

        /// <summary>
        /// Sets a maximum container size INCLUDING the container itself.<br/>
        /// Set to <see langword="null"/> in order to disable any storage limit (not recommended).<br/>
        /// <b>Important:</b> Requires <c>xfs</c> file-system with <c>pquota</c> enabled.<br/>
        /// Read more here: <seealso href="https://docs.docker.com/reference/cli/docker/container/run/#storage-opt"/>
        /// </summary>
        public long? MaxContainerSizeInBytes { get; set; }

        /// <summary>
        /// Sets a maximum payload size.<br/>
        /// Set to <see langword="null"/> in order to disable any payload size storage limit (not recommended if you set <see cref="MaxContainerSizeInBytes"/> to <see langword="null"/>).
        /// </summary>
        public long? MaxPayloadSizeInBytes { get; set; }

        public IList<string>? EnvironmentVariables { get; set; }

        public bool HasVncServer { get; set; }
    }
}
