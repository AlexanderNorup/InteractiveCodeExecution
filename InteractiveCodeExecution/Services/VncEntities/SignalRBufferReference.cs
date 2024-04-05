using MarcusW.VncClient;
using MarcusW.VncClient.Rendering;

namespace InteractiveCodeExecution.Services.VncEntities
{
    public sealed class SignalRBufferReference : IFramebufferReference
    {
        private Size m_size;
        private PixelFormat m_pixelFormat;
        private volatile IntPtr m_address;

        public IntPtr Address => m_address;

        public Size Size => m_size;

        public PixelFormat Format => m_pixelFormat;

        public double HorizontalDpi => m_size.Height;

        public double VerticalDpi => m_size.Width;

        internal SignalRBufferReference(IntPtr address, Size bufferSize, PixelFormat pixelFormat)
        {
            m_size = bufferSize;
            m_pixelFormat = pixelFormat;
            m_address = address;
        }

        public void Dispose()
        {
            ;
        }
    }
}
