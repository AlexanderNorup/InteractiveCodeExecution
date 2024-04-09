using InteractiveCodeExecution.ExecutorEntities;
using InteractiveCodeExecution.Services;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using Microsoft.AspNetCore.SignalR;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

namespace InteractiveCodeExecution.Hubs
{
    public class VncHub : Hub
    {
        private readonly VNCHelper m_vncHelper;
        private readonly IExecutorController m_executorController;

        public VncHub(VNCHelper vncHelper, IExecutorController executorController)
        {
            m_vncHelper = vncHelper ?? throw new ArgumentNullException(nameof(vncHelper));
            m_executorController = executorController ?? throw new ArgumentNullException(nameof(executorController));
        }
        private static Dictionary<string, string> s_connectionToUserIdMapping = new(); // TEMPORARY
        public async Task StartConnection(string userId) //USERID here is temporary!
        {
            s_connectionToUserIdMapping.Add(Context.ConnectionId, userId);
            var allContainers = await m_executorController.GetAllManagedContainersAsync();
            var userContainer = allContainers.FirstOrDefault(x => x.ContainerOwner == userId);

            if (userContainer is null)
            {
                throw new InvalidOperationException("You have no running execution to connect to!");
            }

            if (userContainer.ContainerStreamPort is null)
            {
                throw new InvalidOperationException("This execution cannot be controlled by livestream!");
            }

            try
            {
                await m_vncHelper.Connect(userId, userContainer.ContainerStreamPort.Value, Context.ConnectionAborted);
                await Clients.Caller.SendAsync("ReceiveMessage", "Livestream ready!");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to start stream: {e}");
            }
        }

        public void PerformMouseEvent(int mouseX, int mouseY, bool clickLeft)
        {
            if (!m_vncHelper.TryGetConnection(GetUserId(), out var connection)
                || connection is null)
            {
                return;
            }

            connection.Connection.EnqueueMessage(new PointerEventMessage(new Position(mouseX, mouseY), clickLeft ? MouseButtons.Left : MouseButtons.None));
        }

        public async Task GetScreenshot()
        {
            if (!m_vncHelper.TryGetScreenshot(GetUserId(), out var bitmap)
                || bitmap is null)
            {
                return;
            }

            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            var base64 = Convert.ToBase64String(ms.ToArray());
            await Clients.All.SendAsync("ReceiveScreenshot", "data:image/png;base64," + base64);
        }

        public async IAsyncEnumerable<string> StartLivestream([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            const int Delay = 50;
            string base64;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!m_vncHelper.TryGetScreenshot(GetUserId(), out var bitmap)
                        || bitmap is null)
                    {
                        break;
                    }

                    MemoryStream ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Png);
                    base64 = Convert.ToBase64String(ms.ToArray());
                    yield return "data:image/png;base64," + base64;
                    await Task.Delay(Delay, cancellationToken);
                }
            }
            finally
            {
                await m_vncHelper.CloseConnectionAsync(GetUserId());
                s_connectionToUserIdMapping.Remove(Context.ConnectionId);
            }
        }

        public async Task StopStreaming()
        {
            try
            {
                await m_vncHelper.CloseConnectionAsync(GetUserId());

            }
            catch
            {
                // Don't care   
            }
            s_connectionToUserIdMapping.Remove(Context.ConnectionId);
        }

        private string GetUserId()
        {
            return s_connectionToUserIdMapping[Context.ConnectionId]; //TODO: Replace with auth identifier
        }
    }
}
