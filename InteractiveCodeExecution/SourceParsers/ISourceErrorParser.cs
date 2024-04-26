using InteractiveCodeExecution.ExecutorEntities;

namespace InteractiveCodeExecution.SourceParsers
{
    public interface ISourceErrorParser
    {
        bool TryParseLine(string line, out ExecutionSourceError? executionSourceError);
    }
}
