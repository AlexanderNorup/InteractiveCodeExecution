using Docker.DotNet;
using Docker.DotNet.Models;
using InteractiveCodeExecution.ExecutorEntities;
using Microsoft.Extensions.Options;

namespace InteractiveCodeExecution.Services
{
    public class DockerController : IExecutorController
    {
        private readonly string VolumePath = Path.Combine(AppContext.BaseDirectory, "payloads");
        private readonly DockerConfiguration m_config;
        private readonly ILogger<DockerController> m_logger;
        private DockerClient m_client;
        public DockerController(DockerClientConfiguration configuration, IOptions<DockerConfiguration> options, ILogger<DockerController> logger)
        {
            m_client = configuration.CreateClient();
            m_config = options.Value;
            m_logger = logger;
        }

        public async Task<ExecutorHandle> GetExecutorHandle(ExecutorPayload payload, CancellationToken cancellationToken = default)
        {
            var container = await GetAvailableContainer(payload, cancellationToken);
            Directory.CreateDirectory(container.MountedPath);

            m_logger.LogDebug("Container {Container} ready for loading files!", container.Id);

            // Inject code into the container
            foreach (var file in payload.Files)
            {
                var filepath = Path.Combine(container.MountedPath, file.Filepath); // Security TODO: Ensure there's no path traversal here.
                await File.WriteAllTextAsync(filepath, file.Content, cancellationToken);
            }
            m_logger.LogDebug("Container {Container} primed with {FileCount} files!", container.Id, payload.Files.Count);

            var handle = new ExecutorHandle();

            // Setup handle
            handle.Container = container;
            handle.ShouldBuild = !string.IsNullOrWhiteSpace(payload.BuildCmd);
            handle.BuildStream = async () =>
            {
                m_logger.LogDebug("Building payload for container {Container} with command {BuildCommand}!", container.Id, payload.BuildCmd);
                if (string.IsNullOrWhiteSpace(payload.BuildCmd))
                {
                    throw new InvalidOperationException("This payload does not need to be built");
                }

                var execContainer = await m_client.Exec.ExecCreateContainerAsync(container.Id, new()
                {
                    AttachStderr = true,
                    AttachStdout = true,
                    Tty = false,
                    Cmd = payload.BuildCmd.Split(' '),
                }, cancellationToken).ConfigureAwait(false);

                var buildStream = await m_client.Exec.StartAndAttachContainerExecAsync(execContainer.ID, tty: true, cancellationToken).ConfigureAwait(false);

                var resultAction = async () => await m_client.Exec.InspectContainerExecAsync(execContainer.ID, cancellationToken).ConfigureAwait(false);
                return new DockerStream(buildStream, ExecutionResult.ExecutionStage.Build, resultAction);
            };

            handle.ExecutorStream = async () =>
            {
                m_logger.LogDebug("Executing payload for container {Container} with command {ExecutorCommand}!", container.Id, payload.ExecCmd);
                if (string.IsNullOrWhiteSpace(payload.ExecCmd))
                {
                    throw new ArgumentNullException("The payload executing command is null!");
                }

                var execContainer = await m_client.Exec.ExecCreateContainerAsync(container.Id, new()
                {
                    AttachStderr = true,
                    AttachStdout = true,
                    AttachStdin = true,
                    Tty = false,
                    Cmd = payload.ExecCmd.Split(' '),
                }, cancellationToken).ConfigureAwait(false);

                var execStream = await m_client.Exec.StartAndAttachContainerExecAsync(execContainer.ID, tty: true, cancellationToken).ConfigureAwait(false);

                var resultAction = async () => await m_client.Exec.InspectContainerExecAsync(execContainer.ID, cancellationToken).ConfigureAwait(false);
                return new DockerStream(execStream, ExecutionResult.ExecutionStage.Run, resultAction);
            };

            return handle;
        }

        public async Task ReleaseHandle(ExecutorHandle payload)
        {
            try
            {
                await m_client.Containers.RemoveContainerAsync(payload.Container.Id, new()
                {
                    Force = true,
                }).ConfigureAwait(false);
                Directory.Delete(payload.Container.MountedPath, true);
            }
            catch (DockerContainerNotFoundException)
            {
                // Don't care
            }
        }

        private async Task<ExecutorContainer> GetAvailableContainer(ExecutorPayload payload, CancellationToken cancellationToken = default)
        {
            // TODO: Either return a cached container, or rename the method
            _ = payload.PayloadType ?? throw new ArgumentNullException(nameof(payload.PayloadType));

            if (!m_config.PayloadImageTypeMapping.TryGetValue(payload.PayloadType, out string image))
            {
                throw new ArgumentException($"Unknown {payload.PayloadType} type!");
            }

            const string ContainerPayloadPath = "/payload";
            var mountedPath = Path.Combine(VolumePath, Guid.NewGuid().ToString());
            var container = await m_client.Containers.CreateContainerAsync(new CreateContainerParameters()
            {
                Image = image,
                Labels = new Dictionary<string, string>() {
                {"createdBy", "ScaleableTeaching" },
                {"createdFor", "alnoe20@student.sdu.dk" }
            },
                AttachStderr = true,
                AttachStdin = true,
                AttachStdout = true,
                OpenStdin = true,
                Entrypoint = ["/bin/bash"], // Keeps the container alive, regardless of the command set in the Dockerfile
                HostConfig = new()
                {
                    Binds = [$"{mountedPath}:/{ContainerPayloadPath}"],
                },
                Env = ["DOTNET_LOGGING_CONSOLE_DISABLECOLORS=true"],
                WorkingDir = ContainerPayloadPath
            }, cancellationToken).ConfigureAwait(false); ;

            // Container created
            var started = await m_client.Containers.StartContainerAsync(container.ID, new(), cancellationToken).ConfigureAwait(false); ;

            if (!started)
            {
                throw new Exception($"Failed to start container '{container.ID}'");
            }

            return new ExecutorContainer()
            {
                Id = container.ID,
                MountedPath = mountedPath,
            };
        }
    }
}
