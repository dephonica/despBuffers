using System;
using despBuffers.Interfaces;
using despGlobals;

namespace despBuffers.Buffers
{
    public class SingleBuffer<T> : BaseBuffer<T>
    {
        public override T[] BufferData { get; protected set; }

        public static void InitializeBuffers(string buffersName, SingleBuffer<T>[] buffers, int buffersSize = GlobalConstants.MaxSamplesPerChannelBuffer)
        {
            for (var n = 0; n < buffers.Length; n++)
            {
                buffers[n] = new SingleBuffer<T>(buffersName, buffersSize);
            }
        }

        public SingleBuffer(string bufferName, int samplesPerBuffer = 0)
        {
            Name = bufferName;
            Initialize(samplesPerBuffer);
        }

        private void Initialize(int samplesPerBuffer)
        {
            SetupNull();

            BufferData = new T[samplesPerBuffer];
        }

        public void Push(byte[] buffer, int offset, int bytesLength)
        {
            var floatsToCopy = bytesLength / sizeof(float);
            Ensure(floatsToCopy);

            Buffer.BlockCopy(buffer, offset, BufferData, 0, bytesLength);

            DataLengthSamples = floatsToCopy;
        }

        public override void Push(T[] sourceBuffer, int offset, int sourceLength, DateTime lastTimestamp)
        {
            Ensure(sourceLength);

            DataLengthSamples = sourceLength;

            Array.Copy(sourceBuffer, offset, BufferData, 0, DataLengthSamples);

            LastTimestamp = lastTimestamp;
        }

        public override void Pop(T[] targetBuffer, int offset, int popSamplesCount, out DateTime lastTimestamp)
        {
            lastTimestamp = LastTimestamp;

            if (popSamplesCount > DataLengthSamples)
            {
                throw new BuffersException("Unable to pop more samples than stored in the single buffer");
            }

            Array.Copy(BufferData, 0, targetBuffer, offset, popSamplesCount);
        }

        public override void Push(IAudioBuffer<T> sourceBuffer)
        {
            Ensure(sourceBuffer.DataLengthSamples);

            Inherit(sourceBuffer);

            DataLengthSamples = sourceBuffer.DataLengthSamples;

            Array.Copy(sourceBuffer.BufferData, BufferData, DataLengthSamples);
        }

        public override void Ensure(int desiredSize)
        {
            if (BufferData.Length < desiredSize)
            {
                BufferData = new T[desiredSize];
            }
        }

        public void Trim()
        {
            if (BufferData.Length != DataLengthSamples)
            {
                var trimmedArray = new T[DataLengthSamples];
                Array.Copy(BufferData, trimmedArray, DataLengthSamples);

                BufferData = trimmedArray;
            }
        }

        public void Clear()
        {
            Array.Clear(BufferData, 0, BufferData.Length);
        }

        public void AssignSamples(T[] sourceBuffer)
        {
            AssignSamples(sourceBuffer, sourceBuffer.Length);
        }

        public void AssignSamples(T[] sourceBuffer, int sourceLength)
        {
            BufferData = sourceBuffer;
            DataLengthSamples = sourceLength;
        }

        public void ShiftLeft(int offsetSamples)
        {
            DataLengthSamples -= offsetSamples;
            Array.Copy(BufferData, offsetSamples, BufferData, 0, DataLengthSamples);
        }

        public void ShiftRight(int offsetSamples)
        {
            Ensure(DataLengthSamples + offsetSamples);

            Array.Copy(BufferData, 0, BufferData, offsetSamples, DataLengthSamples);
            Array.Clear(BufferData, 0, offsetSamples);

            DataLengthSamples += offsetSamples;
        }
    }
}
