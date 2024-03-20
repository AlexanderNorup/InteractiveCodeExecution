using Docker.DotNet;
using Docker.DotNet.Models;
using InteractiveCodeExecution.ExecutorEntities;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Formats.Tar;
using System.Text;

namespace InteractiveCodeExecution.Services
{
    public class DockerController : IExecutorController
    {
        private readonly DockerConfiguration m_config;
        private readonly ILogger<DockerController> m_logger;
        private DockerClient m_client;

        private Lazy<Task<bool>> m_doesSupportStorageQouta;

        public DockerController(DockerClientConfiguration configuration, IOptions<DockerConfiguration> options, ILogger<DockerController> logger)
        {
            m_client = configuration.CreateClient();
            m_config = options.Value;
            m_logger = logger;

            // This method will always return the same infrastructure for a given runtime, so we can cache the value of this method using a shared task.
            m_doesSupportStorageQouta = new Lazy<Task<bool>>(() => StorgeQoutaIsSupportedAsync());
        }

        public async Task<ExecutorHandle> GetExecutorHandle(ExecutorPayload payload, ExecutorConfig config, CancellationToken cancellationToken = default)
        {
            // Before getting a container, check if the payload exceeds the maximum allowed
            if (config.MaxPayloadSizeInBytes > 0)
            {
                long numBytes = CalculatePayloadSizeInBytes(payload);
                if (numBytes > config.MaxPayloadSizeInBytes)
                {
                    throw new ExecutorPayloadTooBigException(config.MaxPayloadSizeInBytes.Value, numBytes);
                }
            }

            var container = await GetAvailableContainer(payload, config, cancellationToken);

            m_logger.LogDebug("Container {Container} ready for loading files!", container.Id);

            // Inject code into the container
            var st = Stopwatch.StartNew();
            await UploadPayloadToContainerAsync(payload, container, cancellationToken);
            st.Stop();

            m_logger.LogDebug("Container {Container} primed with {FileCount} files in {Time}!", container.Id, payload.Files.Count, st.Elapsed);

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
            }
            catch (DockerContainerNotFoundException)
            {
                // Don't care
            }
        }

        private async Task<ExecutorContainer> GetAvailableContainer(ExecutorPayload payload, ExecutorConfig config, CancellationToken cancellationToken = default)
        {
            // TODO: Either return a cached container, or rename the method
            _ = payload.PayloadType ?? throw new ArgumentNullException(nameof(payload.PayloadType));

            if (!m_config.PayloadImageTypeMapping.TryGetValue(payload.PayloadType, out var image))
            {
                throw new ArgumentException($"Unknown {payload.PayloadType} type!");
            }

            const string ContainerPayloadPath = "/payload";

            var startParams = new CreateContainerParameters()
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
                    AutoRemove = true,
                    //Binds = [$"{mountedPath}:/{ContainerPayloadPath}"],
                },
                Env = ["DOTNET_LOGGING_CONSOLE_DISABLECOLORS=true"],
                WorkingDir = ContainerPayloadPath,
            };

            if (config.MaxVCpus > 0)
            {
                const long OneVCpuInNanoCpus = 1_000_000_000;
                long nanoCpus = (long)(config.MaxVCpus.Value * OneVCpuInNanoCpus);
                startParams.HostConfig.NanoCPUs = nanoCpus;
            }

            if (config.MaxMemoryBytes > 0)
            {
                startParams.HostConfig.Memory = config.MaxMemoryBytes.Value;
            }

            if (config.MaxContainerSizeInBytes > 0 && await m_doesSupportStorageQouta.Value)
            {
                startParams.HostConfig.StorageOpt = new Dictionary<string, string>()
                {
                    { "size", config.MaxContainerSizeInBytes.Value.ToString() }
                };
            }

            var container = await m_client.Containers.CreateContainerAsync(startParams, cancellationToken).ConfigureAwait(false); ;

            // Container created
            var started = await m_client.Containers.StartContainerAsync(container.ID, new(), cancellationToken).ConfigureAwait(false); ;

            if (!started)
            {
                throw new Exception($"Failed to start container '{container.ID}'");
            }

            return new ExecutorContainer()
            {
                Id = container.ID,
                ContainerPath = ContainerPayloadPath,
            };
        }

        private long CalculatePayloadSizeInBytes(ExecutorPayload payload)
        {
            long totalSize = 0;

            foreach (var file in payload.Files)
            {
                totalSize += file.ContentType switch
                {
                    ExecutorFileType.Base64BinaryFile => (3 * file.Content.Length) / 4, // 3 bytes per 4 characters in base64
                    _ => (long)Encoding.UTF8.GetMaxByteCount(file.Content.Length),
                };
            }

            return totalSize;
        }

        private async Task UploadPayloadToContainerAsync(ExecutorPayload payload, ExecutorContainer container, CancellationToken cancellationToken = default)
        {
            using (var tarBall = new MemoryStream())
            {
                var tarWriter = new TarWriter(tarBall);

                foreach (var file in payload.Files)
                {
                    using var dataStream = new MemoryStream(GetFileContentAsByteArray(file));

                    var tarEntry = new GnuTarEntry(TarEntryType.RegularFile, file.Filepath)
                    {
                        DataStream = dataStream
                    };

                    await tarWriter.WriteEntryAsync(tarEntry, cancellationToken);
                }

                tarBall.Seek(0, SeekOrigin.Begin);
                await m_client.Containers.ExtractArchiveToContainerAsync(container.Id, new()
                {
                    AllowOverwriteDirWithFile = false,
                    Path = container.ContainerPath,
                }, tarBall, cancellationToken);
            }
        }

        private async Task<bool> StorgeQoutaIsSupportedAsync(CancellationToken cancellationToken = default)
        {
            const string RequiredDriver = "overlay2";
            const string BackingSystemKey = "Backing Filesystem";
            const string RequiredBackingSystem = "xfs";

            var info = await m_client.System.GetSystemInfoAsync(cancellationToken);
            var backingDriver = info.DriverStatus.Where(x => x.Length > 0 && x[0].Equals(BackingSystemKey, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            if (info.Driver.Equals(RequiredDriver, StringComparison.OrdinalIgnoreCase)
                && backingDriver is { Length: >= 2 }
                && backingDriver[1].Equals(RequiredBackingSystem, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            m_logger.LogWarning("This system does not support setting a storage quota for Docker containers. This means the " + nameof(ExecutorConfig.MaxContainerSizeInBytes) + " field will be ignored!\n" +
                "The storage driver must be {RequiredDriver} with the backing filesystem of {RequriedBackingFileSystem}.\n" +
                "This system uses the {SystemDriver} wtih the backing filesystem of {SystemBackingFileSystem}\n" +
                "For more info, see https://docs.docker.com/reference/cli/docker/container/run/#storage-opt",
                RequiredDriver, RequiredBackingSystem, info.Driver, backingDriver is { Length: >= 2 } ? backingDriver[1] : "<unknown>");

            return false;
        }

        private static byte[] GetFileContentAsByteArray(ExecutorFile file) => file.ContentType switch
        {
            ExecutorFileType.Base64BinaryFile => Convert.FromBase64String(file.Content),
            _ => Encoding.UTF8.GetBytes(file.Content),
        };
    }
}
