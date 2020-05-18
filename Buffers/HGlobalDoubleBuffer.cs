using System;
using System.Runtime.InteropServices;

namespace despBuffers.Buffers
{
    public class HGlobalDoubleBuffer : IDisposable
    {
        public static int DefaultBufferSize = 65536;

        private int _currentSize;

        public IntPtr Buffer { get; private set; }

        public HGlobalDoubleBuffer(int initialSizeSamples = 0)
        {
            _currentSize = Math.Max(initialSizeSamples * sizeof(double), DefaultBufferSize);
            Buffer = Marshal.AllocHGlobal(_currentSize);
        }

        public void Dispose()
        {
            if (Buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Buffer);
            }
        }

        public void Set(double[] source, int sourceOffset, int samplesToCopy)
        {
            Ensure(samplesToCopy);
            Marshal.Copy(source, sourceOffset, Buffer, samplesToCopy);
        }

        public void Ensure(int samplesToCopy)
        {
            var bytesToCopy = samplesToCopy * sizeof(double);

            if (bytesToCopy > _currentSize)
            {
                Marshal.FreeHGlobal(Buffer);

                _currentSize = bytesToCopy + DefaultBufferSize;
                Buffer = Marshal.AllocHGlobal(_currentSize);
            }
        }
    }
}
