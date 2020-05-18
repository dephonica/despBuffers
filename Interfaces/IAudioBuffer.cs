using System;

namespace despBuffers.Interfaces
{
    public interface IAudioBuffer<T> : IAudioBufferProto
    {
        T[] BufferData { get; }
        new int DataLengthSamples { get; set; }

        void Ensure(int ensureSize);

        void Push(IAudioBuffer<T> inputBuffer);
        void Push(T[] buffer, int offset, int length);
        void Push(T[] buffer, int offset, int length, DateTime lastTimestamp);

        void Pop(T[] buffer, int offset, int length);
        void Pop(T[] buffer, int offset, int length, out DateTime lastTimestamp);
    }
}
