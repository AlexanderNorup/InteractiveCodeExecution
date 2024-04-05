using MarcusW.VncClient;
using MarcusW.VncClient.Rendering;
using System.Collections.Immutable;
using System.Drawing;
using System.Runtime.InteropServices;

namespace InteractiveCodeExecution.Services.VncEntities
{
    public class SignalRRenderTarget : IRenderTarget, IDisposable
    {
        public static readonly MarcusW.VncClient.PixelFormat DefaultFormat = new MarcusW.VncClient.PixelFormat("Plain RGB", 32, 32, bigEndian: true, trueColor: true, hasAlpha: false, 255, 255, 255, 0, 8, 16, 24, 0);

        public bool IsDisposed { get; private set; } = false;

        private object m_lock = new object();
        private bool m_handleAllocated;
        private IntPtr m_handle;
        private SignalRBufferReference? m_buffer;
        public IFramebufferReference GrabFramebufferReference(MarcusW.VncClient.Size size, IImmutableSet<Screen> layout)
        {
            if (!m_handleAllocated || m_buffer is null || m_buffer.Size != size)
            {
                if (m_handleAllocated)
                {
                    Marshal.FreeHGlobal(m_handle);
                }
                m_handle = Marshal.AllocHGlobal(size.Width * size.Height * DefaultFormat.BitsPerPixel);
                m_handleAllocated = true;
            }

            m_buffer = new SignalRBufferReference(m_handle, size, DefaultFormat);
            return m_buffer;
        }

        public Bitmap GetBitmap()
        {
            _ = m_buffer ?? throw new ObjectDisposedException(nameof(SignalRRenderTarget));

            return new Bitmap(m_buffer.Size.Width,
                m_buffer.Size.Height,
                m_buffer.Format.BitsPerPixel / 8 * m_buffer.Size.Width,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb,
                m_buffer.Address);
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                lock (m_lock)
                {
                    if (!IsDisposed)
                    {
                        if (m_handleAllocated)
                        {
                            Marshal.FreeHGlobal(m_handle);
                        }
                        if (m_buffer is not null)
                        {
                            m_buffer.Dispose();
                            m_buffer = null;
                        }
                        IsDisposed = true;
                    }
                }
            }
        }
    }
}
