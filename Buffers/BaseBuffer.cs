using System;
using despBuffers.Channels;
using despBuffers.FlowRider;
using despBuffers.Interfaces;

namespace despBuffers.Buffers
{
    public abstract class BaseBuffer<T> : IAudioBuffer<T>
    {
        public Guid BufferUid { get; } = Guid.NewGuid();

        public string Name { get; protected set; }

        public ChannelGuid OriginUid { get; private set; }
        public ChannelGuid ChannelUid { get; private set; }

        public int Priority { get; private set; }
        public float SampleRate { get; private set; }
        public int Channels { get; private set; } = 1;

        public int SamplesPerChannel => Channels > 0 ? DataLengthSamples / Channels : 0;

        public bool IsInterleaved { get; set; } = true;

        public virtual DateTime LastTimestamp { get; protected set; }

        public DateTime FirstTimestamp
        {
            get
            {
                if (LastTimestamp == DateTime.MinValue ||
                    SampleRate < float.Epsilon)
                {
                    return LastTimestamp;
                }

                return LastTimestamp - TimeSpan.FromSeconds(DataLengthSeconds);
            }
        }

        public float DataLengthSeconds => (float)SamplesToSeconds(DataLengthSamples);

        private int _dataLengthSamples;
        public virtual int DataLengthSamples
        {
            get => _dataLengthSamples;
            set
            {
                _dataLengthSamples = value;
                BuffersFlow.Instance?.OnBufferChanged(this);
            }
        }

        public double SamplesToSeconds(int samplesCount)
        {
            if (SampleRate < float.Epsilon || Channels < 1)
            {
                return 0;
            }

            return (double)samplesCount / SampleRate / Channels;
        }

        public void SetupNull()
        {
            Setup(ChannelGuid.Empty(), ChannelGuid.Empty(), 0, 0, 0);
        }

        public void Setup(ChannelGuid originUid, ChannelGuid channelUid, int priority, float sampleRate, int channels)
        {
            Setup(originUid, channelUid, priority, sampleRate, channels, DateTime.MinValue);
        }

        public void Setup(ChannelGuid originUid, ChannelGuid channelUid, int priority, float sampleRate, int channels, DateTime lastTimestamp)
        {
            if (DataLengthSamples > 0 && (Math.Abs(SampleRate - sampleRate) > double.Epsilon ||
                                          Channels != channels) &&
                                          SampleRate > 0)
            {
                throw new BuffersException($"Unable to change samplerate or channels number for unempty audio buffer '{Name}'");
            }

            OriginUid = originUid;
            ChannelUid = channelUid;
            Priority = priority;
            SampleRate = sampleRate;
            Channels = channels;

            if (lastTimestamp != DateTime.MinValue)
            {
                LastTimestamp = lastTimestamp;
            }

            BuffersFlow.Instance?.OnBufferChanged(this);
        }

        public void AdjustTimestamp(DateTime lastTimestamp)
        {
            if (lastTimestamp != DateTime.MinValue)
            {
                LastTimestamp = lastTimestamp;

                BuffersFlow.Instance?.OnBufferChanged(this);
            }
        }

        public void Inherit(IAudioBufferProto sourceBuffer)
        {
            Setup(sourceBuffer.OriginUid, sourceBuffer.ChannelUid, sourceBuffer.Priority, sourceBuffer.SampleRate, sourceBuffer.Channels, sourceBuffer.LastTimestamp);
        }

        public void Push(T[] buffer, int offset, int length)
        {
            Push(buffer, offset, length, DateTime.MinValue);
        }

        public void Pop(T[] buffer, int offset, int length)
        {
            Pop(buffer, offset, length, out DateTime _);
        }

        public abstract void Push(IAudioBuffer<T> inputBuffer);
        public abstract void Push(T[] buffer, int offset, int length, DateTime lastTimestamp);
        public abstract void Pop(T[] buffer, int offset, int length, out DateTime lastTimestamp);

        public abstract T[] BufferData { get; protected set; }
        public abstract void Ensure(int ensureSize);
    }
}
