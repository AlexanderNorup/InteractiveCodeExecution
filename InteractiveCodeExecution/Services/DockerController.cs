using Docker.DotNet;
using Docker.DotNet.Models;
using InteractiveCodeExecution.ExecutorEntities;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Formats.Tar;
using System.Reflection.Metadata;
using System.Text;

namespace InteractiveCodeExecution.Services
{
    public class DockerController : IExecutorController
    {
        private readonly DockerConfiguration m_config;
        private readonly ILogger<DockerController> m_logger;
        private DockerClient m_client;
        private VNCHelper m_vncClient;

        private List<ExecutorContainer> m_containers = new();
        private readonly ConcurrentBag<int> m_vncHostPortPool;

        private Lazy<Task<bool>> m_doesSupportStorageQouta;

        public DockerController(DockerClientConfiguration configuration, IOptions<DockerConfiguration> options, VNCHelper vncHelper, ILogger<DockerController> logger)
        {
            m_client = configuration.CreateClient();
            m_vncClient = vncHelper;
            m_config = options.Value;
            m_logger = logger;

            m_vncHostPortPool = new(m_config.AvailableVncPortNumbers);

            // This method will always return the same infrastructure for a given runtime, so we can cache the value of this method using a shared task.
            m_doesSupportStorageQouta = new Lazy<Task<bool>>(() => StorgeQoutaIsSupportedAsync());
        }

        public async Task<ExecutorHandle> GetExecutorHandle(ExecutorPayload payload, ExecutorAssignment assignment, ExecutorConfig config, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(assignment.AssignmentId)
                || string.IsNullOrWhiteSpace(assignment.Image))
            {
                throw new Exception("Improper setup of assignments. AssignmentId or Image is invalid");
            }

            // Before getting a container, check if the payload exceeds the maximum allowed
            if (config.MaxPayloadSizeInBytes > 0)
            {
                long numBytes = PayloadUtils.CalculatePayloadSizeInBytes(payload);
                if (numBytes > config.MaxPayloadSizeInBytes)
                {
                    throw new ExecutorPayloadTooBigException(config.MaxPayloadSizeInBytes.Value, numBytes);
                }
            }

            var container = await GetAvailableContainer(assignment.Image, config, userId, cancellationToken);
            m_containers.Add(container);

            m_logger.LogDebug("Container {Container} ready for loading files!", container.Id);

            // Inject code into the container
            var st = Stopwatch.StartNew();
            await UploadPayloadToContainerAsync(payload, container, cancellationToken);
            st.Stop();

            m_logger.LogDebug("Container {Container} primed with {FileCount} files in {Time}!", container.Id, payload.Files.Count, st.Elapsed);

            var handle = new ExecutorHandle();

            // Setup handle
            handle.Container = container;
            handle.HasBuildSteps = assignment.Commands.Any(x => x.Stage == ExecutorCommand.ExecutorStage.Build);

            foreach (var cmd in assignment.Commands)
            {
                if (payload.BuildOnly && cmd.Stage == ExecutorCommand.ExecutorStage.Exec)
                {
                    continue;
                }
                handle.ExecutorStreams.Add(
                    (!cmd.WaitForExit,
                    async () =>
                    {
                        m_logger.LogDebug("Executing payload for container {Container} with command {Command} (RunInBackground={RunInBackGround})!", container.Id, cmd.Command, cmd.WaitForExit);
                        if (string.IsNullOrWhiteSpace(cmd.Command))
                        {
                            throw new ArgumentNullException("The payload executing command is null!");
                        }

                        var execContainer = await m_client.Exec.ExecCreateContainerAsync(container.Id, new()
                        {
                            AttachStderr = cmd.WaitForExit,
                            AttachStdout = cmd.WaitForExit,
                            AttachStdin = cmd.WaitForExit,
                            Tty = false,
                            Cmd = cmd.Command.Split(' '),
                        }, cancellationToken).ConfigureAwait(false);

                        if (!cmd.WaitForExit)
                        {
                            await m_client.Exec.StartContainerExecAsync(execContainer.ID, cancellationToken).ConfigureAwait(false);
                            // Don't wait for exit, so we don't attach to the container
                            return null;
                        }

                        var execStream = await m_client.Exec.StartAndAttachContainerExecAsync(execContainer.ID, tty: false, cancellationToken).ConfigureAwait(false);

                        var resultAction = async () => await m_client.Exec.InspectContainerExecAsync(execContainer.ID, cancellationToken).ConfigureAwait(false);
                        return new DockerStream(execStream, cmd.Stage, resultAction);
                    }
                ));
            }

            return handle;
        }

        public async Task ReleaseHandle(ExecutorHandle payload)
        {
            // If a VNC connection might be active, try to close it down first
            if (payload.Container.ContainerStreamPort is not null)
            {
                try
                {
                    await m_vncClient.CloseConnectionAsync(payload.Container.ContainerOwner);
                }
                catch (Exception)
                {
                    // Don't care
                }
                m_vncHostPortPool.Add(payload.Container.ContainerStreamPort.Value);
            }

            try
            {
                m_containers.RemoveAll(container => container.Id == payload.Container.Id);
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

        public async Task<IList<ExecutorContainer>> GetAllManagedContainersAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(new List<ExecutorContainer>(m_containers));
        }

        private async Task<ExecutorContainer> GetAvailableContainer(string image, ExecutorConfig config, string userId, CancellationToken cancellationToken = default)
        {
            if (!await EnsureLocalImagePresent(image))
            {
                throw new Exception($"Cannot find image '{image}'");
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

            int? hostVncPort = null;
            if (config.HasVncServer)
            {
                if (!m_vncHostPortPool.TryTake(out var hostPort) || hostPort == default)
                {
                    throw new Exception("Failed to reserve host port for screen connection.");
                }
                hostVncPort = hostPort;
                startParams.HostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>()
                {
                    { VNCHelper.DefaultVncPort.ToString() + "/tcp", new List<PortBinding>(){ new PortBinding() { HostPort = hostVncPort.ToString() } } },
                };
                startParams.ExposedPorts = new Dictionary<string, EmptyStruct>()
                {
                    { VNCHelper.DefaultVncPort.ToString() + "/tcp", new() },
                };
            }

            if (config.EnvironmentVariables is { Count: > 0 } envs)
            {
                startParams.Env = new List<string>(startParams.Env.Concat(envs));
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
                ContainerOwner = userId,
                ContainerStreamPort = hostVncPort
            };
        }

        private async Task UploadPayloadToContainerAsync(ExecutorPayload payload, ExecutorContainer container, CancellationToken cancellationToken = default)
        {
            if (payload.Files is null || !payload.Files.Any())
            {
                return;
            }
            using (var tarBall = new MemoryStream())
            {
                await PayloadUtils.WritePayloadToTarball(payload, tarBall, cancellationToken).ConfigureAwait(false);
                await m_client.Containers.ExtractArchiveToContainerAsync(container.Id, new()
                {
                    AllowOverwriteDirWithFile = false,
                    Path = container.ContainerPath,
                }, tarBall, cancellationToken).ConfigureAwait(false);
            }
        }

        private static ConcurrentDictionary<string, bool> s_imageCache = new ConcurrentDictionary<string, bool>();
        private async Task<bool> EnsureLocalImagePresent(string image, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(image))
            {
                return false;
            }

            if (s_imageCache.TryGetValue(image, out var exists))
            {
                return exists;
            }

            try
            {
                await m_client.Images.InspectImageAsync(image, cancellationToken).ConfigureAwait(false);
                s_imageCache.TryAdd(image, true);
                return true;
            }
            catch (DockerImageNotFoundException)
            {
                m_logger.LogInformation("Could not find image {Image}. Attempting to pull!", image);
                try
                {
                    var progressHandler = new ConsoleProgress(image, m_logger);

                    await m_client.Images.CreateImageAsync(new() { FromImage = image }, new(), progressHandler, cancellationToken);

                    m_logger.LogInformation("Image {Image} successfully pulled!!", image);
                    s_imageCache.TryAdd(image, true);
                    return true;
                }
                catch (Exception e)
                {
                    m_logger.LogError(e, "Image {Image} successfully pulled!!", image);
                    s_imageCache.TryAdd(image, false); // We can't find this image
                    return true;
                }
            }

        }

        private async Task<bool> StorgeQoutaIsSupportedAsync(CancellationToken cancellationToken = default)
        {
            const string RequiredDriver = "overlay2";
            const string BackingSystemKey = "Backing Filesystem";
            const string RequiredBackingSystem = "xfs";

            var info = await m_client.System.GetSystemInfoAsync(cancellationToken).ConfigureAwait(false);
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
    }

    internal class ConsoleProgress : IProgress<JSONMessage>
    {
        public ILogger<DockerController> m_logger;
        public string m_name;
        public ConsoleProgress(string imageName, ILogger<DockerController> logger)
        {
            m_name = imageName ?? throw new ArgumentNullException(nameof(imageName));
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public void Report(JSONMessage value)
        {
            m_logger.LogDebug("Downloading {ImageName}: {Status}!", m_name, value.Status);
        }
    }
}
