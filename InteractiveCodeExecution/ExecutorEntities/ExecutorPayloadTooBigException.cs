namespace InteractiveCodeExecution.ExecutorEntities
{
    public class ExecutorPayloadTooBigException : Exception
    {
        public long MaximumBytesAllowed { get; }
        public long NumberOfBytesInPayload { get; }

        public ExecutorPayloadTooBigException(long maximumBytesAllowed, long numberOfBytesInPayload)
            : base($"Maximum payload size exceeded by {numberOfBytesInPayload - maximumBytesAllowed} bytes! " +
                  $"Maximum allowed size is {maximumBytesAllowed} bytes. Payload size was {numberOfBytesInPayload} bytes")
        {
            MaximumBytesAllowed = maximumBytesAllowed;
            NumberOfBytesInPayload = numberOfBytesInPayload;
        }
    }
}
