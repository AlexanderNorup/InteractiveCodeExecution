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
                AssignmentId = "CSharpTwoStep",
                AssignmentName = "Two-step CSharp with dotnet 8",
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
                AssignmentId = "VNC Base",
                AssignmentName = "The VNC Base-image",
                Image = "ghcr.io/alexandernorup/interactivecodeexecution/vnc_base_image:v1", // From the /VncDockerImages/VncBase.Dockerfile
                Commands = new List<ExecutorCommand>()
                {
                    new()
                    {
                        Command = "/run_vnc.sh",
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
                        "DISPLAY=:0"
                    },
                    MaxMemoryBytes = 1024 * 1024 * 512L,
                    MaxVCpus = 0.5,
                    Timeout = TimeSpan.FromMinutes(5),
                    MaxPayloadSizeInBytes = 300,
                    HasVncServer = true
                },
                InitialPayload = new ()
                {
                    new ()
                    {
                        Content = "The example is ready. There are no files for this one, so this is a just a placeholder file!",
                        ContentType = ExecutorFileType.Utf8TextFile,
                        Filepath = "Hello.txt"
                    },
                }
            },
            new ExecutorAssignment()
            {
                AssignmentId = "vop-f24_point-giving-activity-1",
                AssignmentName = "VOP-24 Point giving activity 1",
                Image = "ghcr.io/alexandernorup/interactivecodeexecution/vnc_java:22-javafx", // From the /VncDockerImages/Java22Vnc.Dockerfile
                Commands = new List<ExecutorCommand>()
                {
                    new()
                    {
                        Command = "/run_vnc.sh",
                        Stage = ExecutorCommand.ExecutorStage.Exec,
                        WaitForExit = false
                    },
                    new()
                    {
                        Command = "sleep 4",
                        Stage = ExecutorCommand.ExecutorStage.Exec,
                        WaitForExit = true
                    },
                    new()
                    {
                        Command = "mvn javafx:run",
                        Stage = ExecutorCommand.ExecutorStage.Exec,
                        WaitForExit = true
                    },
                },
                ExecutorConfig = new()
                {
                    EnvironmentVariables = new List<string>()
                    {
                        "RESOLUTION=854x480",
                        "DISPLAY=:0"
                    },
                    MaxMemoryBytes = 1024 * 1024 * 512L,
                    MaxVCpus = 0.5,
                    Timeout = TimeSpan.FromMinutes(5),
                    MaxPayloadSizeInBytes = 1024*1024*5, // 5 Megabytes
                    HasVncServer = true
                },
                InitialPayload = GetExecutorFilesFromDirectory(Path.Combine(AppContext.BaseDirectory, "point-giving-activity-1"))
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

        private static List<ExecutorFile> GetExecutorFilesFromDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                // We could throw here.
                // We don't because this is a PoC and the directory might legitimately not exist
                return new() {
                    new()
                    {
                        Content = $"This deployment has not been configured for this assignment.\nI except a directory to be present at \"{path}\" that contains the files for this assignment.",
                        ContentType = ExecutorFileType.Utf8TextFile,
                        Filepath = "error.txt"
                    }
                };
            }

            var files = new List<ExecutorFile>();

            RecurisvelyAddFilesFromDirectory(path, files);

            return files;
        }

        private static readonly HashSet<string> KnownBinaryFormats = new()
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp",
            ".mov",
            ".mp4",
            ".pdf",
            ".bin",
            ".jar",
            ".exe",
            ".tar",
            ".gz",
            ".zip"
        };

        private static void RecurisvelyAddFilesFromDirectory(string path, List<ExecutorFile> files, string containerPath = "", int depth = 0)
        {
            if (depth > 50)
            {
                throw new InvalidDataException("Execute folder structure too deep!");
            }

            foreach (var file in Directory.EnumerateFileSystemEntries(path))
            {
                // This is the path inside the container. This does not use Path.Combine() because we always want to use "/" for separators in the container.
                var containerPathFile = containerPath + "/" + Path.GetFileName(file);

                if (Directory.Exists(file))
                {
                    RecurisvelyAddFilesFromDirectory(file, files, containerPathFile, depth + 1);
                    continue;
                }
                else if (File.Exists(file))
                {
                    var pathWithoutLeadingSlash = containerPathFile.TrimStart('/');
                    if (KnownBinaryFormats.Contains(Path.GetExtension(file)))
                    {
                        files.Add(new()
                        {
                            Content = Convert.ToBase64String(File.ReadAllBytes(file)),
                            ContentType = ExecutorFileType.Base64BinaryFile,
                            Filepath = pathWithoutLeadingSlash,
                        });
                    }
                    else
                    {
                        files.Add(new()
                        {
                            Content = File.ReadAllText(file),
                            ContentType = ExecutorFileType.Utf8TextFile,
                            Filepath = pathWithoutLeadingSlash,
                        });
                    }
                }
            }
        }
    }
}
