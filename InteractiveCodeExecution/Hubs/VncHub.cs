﻿using InteractiveCodeExecution.ExecutorEntities;
using InteractiveCodeExecution.Services;
using InteractiveCodeExecution.VncEvents;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using Microsoft.AspNetCore.SignalR;
using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Threading;

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
                await Clients.Caller.SendAsync("ReceiveMessage", "Preparing livestream...");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to start stream: {e}");
            }
        }

        public void PerformMouseEvent(VncMouseEvent mouseEvent)
        {
            if (!m_vncHelper.TryGetConnection(GetUserId(), out var connection)
                || connection is null)
            {
                return;
            }

            var pos = new Position(mouseEvent.MouseX, mouseEvent.MouseY);
            var buttons = GetMouseButtons(mouseEvent);

            connection.Connection.EnqueueMessage(new PointerEventMessage(pos, buttons));
        }

        public void PerformKeyboardEvent(VncKeyboardEvent keyboardEvent)
        {
            if (!m_vncHelper.TryGetConnection(GetUserId(), out var connection)
                || connection is null)
            {
                return;
            }

            if (Enum.IsDefined(typeof(KeySymbol), keyboardEvent.UnicodeKey))
            {
                connection.Connection.EnqueueMessage(new KeyEventMessage(keyboardEvent.KeyPressed, (KeySymbol)keyboardEvent.UnicodeKey));
            }
        }

        public async Task GetScreenshot()
        {
            if (!m_vncHelper.TryGetScreenshot(GetUserId(), out var bitmap)
                || bitmap is null)
            {
                return;
            }

            MemoryStream ms = new MemoryStream();
            bitmap.Encode(ms, SkiaSharp.SKEncodedImageFormat.Jpeg, 60);
            var base64 = Convert.ToBase64String(ms.ToArray());
            bitmap.Dispose();
            await Clients.All.SendAsync("ReceiveScreenshot", "data:image/png;base64," + base64);
        }

        public async IAsyncEnumerable<byte[]> StartLivestream([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using MemoryStream ms = new MemoryStream();
            const int Delay = 50;
            string base64;
            SKBitmap? bitmapReference = null;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!m_vncHelper.TryGetScreenshot(GetUserId(), out bitmapReference)
                        || bitmapReference is null)
                    {
                        break;
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    bitmapReference.Encode(ms, SKEncodedImageFormat.Jpeg, 60);
                    yield return ms.ToArray();
                    bitmapReference.Dispose();
                    await Task.Delay(Delay, cancellationToken);
                }
            }
            finally
            {
                await m_vncHelper.CloseConnectionAsync(GetUserId());
                s_connectionToUserIdMapping.Remove(Context.ConnectionId);
                bitmapReference?.Dispose();
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
            try
            {
                return s_connectionToUserIdMapping[Context.ConnectionId]; //TODO: Replace with auth identifier
            }catch (Exception)
            {
                return string.Empty;
            }
        }

        private MouseButtons GetMouseButtons(VncMouseEvent mouseEvent)
        {
            MouseButtons buttons = MouseButtons.None;

            if (mouseEvent.LeftMouse)
            {
                buttons |= MouseButtons.Left;
            }

            if (mouseEvent.RightMouse)
            {
                buttons |= MouseButtons.Right;
            }

            if (mouseEvent.MiddleMouse)
            {
                buttons |= MouseButtons.Middle;
            }

            if (mouseEvent.ScrollDown)
            {
                buttons |= MouseButtons.WheelDown;
            }

            if (mouseEvent.ScrollUp)
            {
                buttons |= MouseButtons.WheelUp;
            }

            return buttons;
        }
    }
}
