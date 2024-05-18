using InteractiveCodeExecution.ExecutorEntities;

namespace InteractiveCodeExecution.Services
{
    public class PoCAssignmentProvider : IExecutorAssignmentProvider
    {
        public readonly List<ExecutorAssignment> Assignments = new List<ExecutorAssignment>()
        {
            new ExecutorAssignment()
            {
                AssignmentId = "CSharpHello",
                AssignmentName = "Simple CSharp with dotnet 8",
                Image = "mcr.microsoft.com/dotnet/sdk:8.0",
                Commands = new List<ExecutorCommand>()
                {
                    new()
                    {
                        Command = "dotnet build",
                        Stage = ExecutorCommand.ExecutorStage.Build,
                        WaitForExit = true
                    },
                    new()
                    {
                        Command = "dotnet run",
                        Stage = ExecutorCommand.ExecutorStage.Exec,
                        WaitForExit = true
                    }
                },
                InitialPayload = new()
                {
                    new()
                    {
                        Content = "Console.WriteLine(\"Hello World!\");",
                        ContentType = ExecutorFileType.Utf8TextFile,
                        Filepath = "Program.cs"
                    },
                    new()
                    {
                        Content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>",
                        ContentType = ExecutorFileType.Utf8TextFile,
                        Filepath = "Project.csproj"
                    }
                }
            },
            new ExecutorAssignment()
            {
                AssignmentId = "PythonHello",
                AssignmentName = "Plain python 3.13",
                Image = "python:3.13-rc-bullseye",
                Commands = new List<ExecutorCommand>()
                {
                    new()
                    {
                        Command = "python main.py",
                        Stage = ExecutorCommand.ExecutorStage.Exec,
                        WaitForExit = true
                    },
                },
                InitialPayload = new()
                {
                    new()
                    {
                        Content = "print(\"Hello World!\")",
                        ContentType = ExecutorFileType.Utf8TextFile,
                        Filepath = "main.py"
                    },
                }
            },
            new ExecutorAssignment()
            {
                AssignmentId = "VNCTest",
                AssignmentName = "A very cool VNC example",
                Image = "elestio/docker-desktop-vnc:V1",
                Commands = new List<ExecutorCommand>()
                {
                    new()
                    {
                        Command = "bash /startup.sh",
                        Stage = ExecutorCommand.ExecutorStage.Exec,
                        WaitForExit = false
                    },
                    new()
                    {
                        Command = "sleep 5",
                        Stage = ExecutorCommand.ExecutorStage.Exec,
                        WaitForExit = true
                    },
                    new()
                    {
                        Command = "firefox",
                        Stage = ExecutorCommand.ExecutorStage.Exec,
                        WaitForExit = true
                    },
                },
                ExecutorConfig = new()
                {
                    EnvironmentVariables = new List<string>()
                    {
                        "RESOLUTION=854x480",
                        "DISPLAY=:1.0"
                    },
                    HasVncServer = true
                },
                InitialPayload = new()
                {
                    new()
                    {
                        Content = "The example is ready. There are no files for this one, so this is a just a placeholder file!",
                        ContentType = ExecutorFileType.Utf8TextFile,
                        Filepath = "Hello.txt"
                    },
                }
            }
        };

        public IEnumerable<ExecutorAssignment> GetAllAssignments()
        {
            return Assignments;
        }

        public bool TryGetAssignment(string assignmentId, out ExecutorAssignment? assignment)
        {
            assignment = Assignments.FirstOrDefault(x => x.AssignmentId == assignmentId);
            return assignment is not null;
        }
    }
}
