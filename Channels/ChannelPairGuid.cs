using System;

namespace despBuffers.Channels
{
    [Serializable]
    public struct ChannelPairGuid : IComparable, IEquatable<ChannelPairGuid>
    {
        public ChannelGuid InputChannel { get; }
        public ChannelGuid OutputChannel { get; }

        public ChannelPairGuid(ChannelGuid inputChannel, ChannelGuid outputChannel)
        {
            InputChannel = inputChannel;
            OutputChannel = outputChannel;
        }

        public static ChannelPairGuid Empty()
        {
            return new ChannelPairGuid(ChannelGuid.Empty(), ChannelGuid.Empty());
        }

        public override int GetHashCode()
        {
            return InputChannel.GetHashCode() ^ OutputChannel.GetHashCode();
        }

        public bool Equals(ChannelPairGuid reference)
        {
            return CompareTo(reference) == 0;
        }

        public int CompareTo(object reference)
        {
            if ((reference is ChannelPairGuid) == false)
            {
                throw new ArgumentException(
                    $@"Unable to compare with unequivalent type: {reference.GetType()}", nameof(reference));
            }

            var compareTo = (ChannelPairGuid) reference;

            var firstCompare = InputChannel.CompareTo(compareTo.InputChannel);
            if (firstCompare != 0)
            {
                return firstCompare;
            }

            return OutputChannel.CompareTo(compareTo.OutputChannel);
        }

        public override string ToString()
        {
            return InputChannel.ToString() + OutputChannel;
        }

        public static ChannelPairGuid Parse(string source)
        {
            return new ChannelPairGuid(
                new ChannelGuid(source.Substring(0, source.Length / 2)),
                new ChannelGuid(source.Substring(source.Length / 2)));
        }
    }
}
