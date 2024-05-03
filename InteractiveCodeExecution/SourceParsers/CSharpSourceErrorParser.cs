using InteractiveCodeExecution.ExecutorEntities;
using System.Text.RegularExpressions;

namespace InteractiveCodeExecution.SourceParsers
{
    public class CSharpSourceErrorParser : ISourceErrorParser
    {
        // Improvement: This regex has hardcoded what the payload path is. Ideally, it should pull that from somewhere central where DockerController also pulls it
        // https://regex101.com/r/XimKxn/3
        private static Regex DotnetErrorParser = new Regex("^\\/payload\\/(?'filePath'.+)\\((?'lineNum'\\d+),(?'columnNum'\\d+)\\).*(?'type'error|warning)\\s?(?'code'.*):(?'message'.*)", RegexOptions.Compiled);

        public bool TryParseLine(string line, out ExecutionSourceError? executionSourceError)
        {
            executionSourceError = null;

            if (DotnetErrorParser.Match(line) is { Groups.Count: 7 } match)
            {
                var groups = match.Groups;
                executionSourceError = new()
                {
                    AffectedFile = groups[1].Value,
                    Line = TryParseIntOrNull(groups[2].Value),
                    Column = TryParseIntOrNull(groups[3].Value),
                    Type = groups[4].Value,
                    ErrorCode = groups[5].Value.Trim(),
                    ErrorMessage = groups[6].Value.Trim(),
                };
                return true;
            }

            return false;
        }

        private static int? TryParseIntOrNull(string input)
        {
            if (int.TryParse(input, out int parsed))
            {
                return parsed;
            }
            return null;
        }
    }
}
