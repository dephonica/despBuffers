using System;
using despBuffers.Channels;

namespace despBuffers.Interfaces
{
    public interface IAudioBufferProto
    {
        Guid BufferUid { get; }
        ChannelGuid OriginUid { get; }
        ChannelGuid ChannelUid { get; }

        string Name { get; }

        bool IsInterleaved { get; }

        int Priority { get; }
        float SampleRate { get; }
        int Channels { get; }
        int DataLengthSamples { get; }

        DateTime LastTimestamp { get; }

        void Setup(ChannelGuid originUid, ChannelGuid channelUid, int priority, float sampleRate, int channels);
        void Setup(ChannelGuid originUid, ChannelGuid channelUid, int priority, float sampleRate, int channels, DateTime lastTimestamp);

        void Inherit(IAudioBufferProto sourceBuffer);
        void AdjustTimestamp(DateTime lastTimestamp);
    }
}
