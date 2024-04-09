using InteractiveCodeExecution.Services.VncEntities;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
using System.Collections.Concurrent;
using System.Drawing;

namespace InteractiveCodeExecution.Services
{
    public class VNCHelper : IDisposable
    {
        public const int DefaultVncPort = 5900;

        private readonly ILoggerFactory m_logger;

        private ConcurrentDictionary<string, VncConnection> m_activeConnections;
        private bool disposedValue;

        public VNCHelper(ILoggerFactory loggerFactory)
        {
            m_activeConnections = new();
            m_logger = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task Connect(string userId, int port, CancellationToken cancellationToken = default)
        {
            // Create a vnc client object. This can be used to easily start new connections.
            var vncClient = new VncClient(m_logger);
            var renderTarget = new SignalRRenderTarget();

            // Create a new authentication handler to handle authentication requests from the server
            //var authenticationHandler = new DemoAuthenticationHandler();
            var parameters = new ConnectParameters
            {
                TransportParameters = new TcpTransportParameters
                {
                    Host = "localhost",
                    Port = port
                },
                InitialRenderTarget = renderTarget,
                AuthenticationHandler = new SignalRAuthenticationHandler(),
                //AuthenticationHandler = authenticationHandler
                // There are many more parameters to explore...
            };

            // Start a new connection and save the returned connection object
            var connection = await vncClient.ConnectAsync(parameters, cancellationToken).ConfigureAwait(true);
            m_activeConnections.AddOrUpdate(userId, new VncConnection(connection, renderTarget), (key, oldValue) =>
            {
                oldValue.Connection.Dispose();
                oldValue.RenderTarget.Dispose();
                return new(connection, renderTarget);
            });
        }

        internal bool TryGetConnection(string userId, out VncConnection? connection)
        {
            connection = null;
            if (m_activeConnections.TryGetValue(userId, out var con))
            {
                connection = con;
                return true;
            }
            return false;
        }

        internal VncConnection GetConnection(string userId)
        {
            if (!m_activeConnections.TryGetValue(userId, out var con))
            {
                throw new InvalidOperationException($"The user {userId} does not have any active connections");
            }
            return con;
        }

        public bool TryGetScreenshot(string userId, out Bitmap? bitmap)
        {
            bitmap = null;
            if (!TryGetConnection(userId, out var connection)
                || connection is null)
            {
                return false;
            }

            var vncConnection = GetConnection(userId);
            EnsureNonDisposedRenderTarget(vncConnection);
            bitmap = vncConnection.RenderTarget.GetBitmap();
            return true;
        }

        public Bitmap GetScreenshot(string userId)
        {
            var vncConnection = GetConnection(userId);
            EnsureNonDisposedRenderTarget(vncConnection);

            return vncConnection.RenderTarget.GetBitmap();
        }

        public async Task CloseConnectionAsync(string userId)
        {
            if (!m_activeConnections.TryGetValue(userId, out var con))
            {
                return;
            }

            try
            {
                await con.Connection.CloseAsync();
            }
            catch (Exception)
            {
                // Don't care
            }

            m_activeConnections.Remove(userId, out _);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var vncConnection in m_activeConnections.Values)
                    {
                        vncConnection.Connection.Dispose();
                        vncConnection.RenderTarget.Dispose();
                    }
                    m_activeConnections.Clear();
                    m_logger?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void EnsureNonDisposedRenderTarget(VncConnection connection)
        {
            if (connection.RenderTarget.IsDisposed)
            {
                var newRenderTarget = new SignalRRenderTarget();
                connection.Connection.RenderTarget = newRenderTarget;
                connection.RenderTarget = newRenderTarget;
            }
        }

        internal class VncConnection
        {
            internal RfbConnection Connection { get; }
            internal SignalRRenderTarget RenderTarget { get; set; }

            public VncConnection(RfbConnection connection, SignalRRenderTarget renderTarget)
            {
                Connection = connection ?? throw new ArgumentNullException(nameof(connection));
                RenderTarget = renderTarget ?? throw new ArgumentNullException(nameof(renderTarget));
            }
        }
    }
}
