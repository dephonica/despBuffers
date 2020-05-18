using System;

namespace despBuffers.Channels
{
    [Serializable]
    public struct ChannelGuid : IComparable, IEquatable<ChannelGuid>
    {
        private static readonly int GuidLength = Guid.Empty.ToString("N").Length;
        private const int ChannelIndexSerialLength = 5;

        public Guid DeviceGuid { get; set; }    // Set must be public for deserialize
        public int ChannelIndex { get; set; }   // Set must be public for deserialize

        public static ChannelGuid Empty()
        {
            return new ChannelGuid(Guid.Empty, -1);
        }

        public ChannelGuid(string serialGuid)
        {
            if (serialGuid.Length != GuidLength + ChannelIndexSerialLength)
            {
                throw new Exception("Unable to create ChannelGuid from malformed serial guid string");
            }

            DeviceGuid = new Guid(serialGuid.Substring(0, GuidLength));
            ChannelIndex = int.Parse(serialGuid.Substring(GuidLength));
        }

        public ChannelGuid(Guid deviceGuid, int channelIndex)
        {
            DeviceGuid = deviceGuid;
            ChannelIndex = channelIndex;
        }

        public override int GetHashCode()
        {
            return DeviceGuid.GetHashCode() ^ ChannelIndex.GetHashCode();
        }

        public bool Equals(ChannelGuid reference)
        {
            return CompareTo(reference) == 0;
        }

        public int CompareTo(object reference)
        {
            if (reference is ChannelGuid == false)
            {
                throw new ArgumentException(
                    $@"Unable to compare with unequivalent type: {reference.GetType()}", nameof(reference));
            }

            var compareTo = (ChannelGuid) reference;

            var guidCompare = DeviceGuid.CompareTo(compareTo.DeviceGuid);
            return guidCompare != 0 ? guidCompare : ChannelIndex.CompareTo(compareTo.ChannelIndex);
        }

        public override string ToString()
        {
            return DeviceGuid.ToString("N") + ChannelIndex.ToString("D5");
        }

        public static ChannelGuid Parse(string source)
        {
            return new ChannelGuid(source);
        }
    }
}
