using MarcusW.VncClient;
using MarcusW.VncClient.Rendering;
using SkiaSharp;
using System.Collections.Immutable;
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

        public SKBitmap? GetBitmap()
        {
            _ = m_buffer ?? throw new ObjectDisposedException(nameof(SignalRRenderTarget));

            var bufferLength = m_buffer.Format.BitsPerPixel * m_buffer.Size.Width * m_buffer.Size.Height;
            if (bufferLength <= 0)
            {
                return null;
            }

            var sBitmap = new SKBitmap();
            sBitmap.InstallPixels(new SKImageInfo(m_buffer.Size.Width,
                                        m_buffer.Size.Height,
                                        SKColorType.Bgra8888,
                                        SKAlphaType.Opaque)
                                    , m_buffer.Address);

            return sBitmap;
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
