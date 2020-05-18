using System;
using despBuffers.Interfaces;

namespace despBuffers.FlowRider
{
    [Serializable]
    public class BufferFlowDescription : IEquatable<BufferFlowDescription>
    {
        private static readonly object LockTimestamp = new object();
        private static DateTime _previousTimestamp = DateTime.MinValue;
        private static int _timestampDiversityCounter = 1;

        public DateTime Timestamp = GetTimestamp();

        public int InfoId;
        public string Info;

        public int Priority;
        public float SampleRate;
        public int Channels;
        public int DataLength;
        public DateTime BufferLastTimestamp;

        private static DateTime GetTimestamp()
        {
            lock (LockTimestamp)
            {
                var currentTimestamp = DateTime.Now;
                var resultTimestamp = currentTimestamp;

                if (resultTimestamp == _previousTimestamp)
                {
                    _timestampDiversityCounter++;
                    resultTimestamp = currentTimestamp + TimeSpan.FromTicks(_timestampDiversityCounter);
                }
                else
                {
                    _timestampDiversityCounter = 1;
                }

                if (resultTimestamp == _previousTimestamp)
                {
                    throw new BuffersException("BufferFlowDescription.GetTimestamp is not working");
                }

                _previousTimestamp = currentTimestamp;

                return resultTimestamp;
            }
        }

        public static BufferFlowDescription FromSource(IAudioBufferProto source)
        {
            return new BufferFlowDescription
            {
                Priority = source.Priority,
                SampleRate = source.SampleRate,
                Channels = source.Channels,
                DataLength = source.DataLengthSamples,
                BufferLastTimestamp = source.LastTimestamp
            };
        }

        public bool Equals(BufferFlowDescription toObject)
        {
            if (toObject == null ||
                toObject.Priority != Priority ||
                Math.Abs(toObject.SampleRate - SampleRate) > float.Epsilon ||
                toObject.Channels != Channels ||
                toObject.DataLength != DataLength)
            {
                return false;
            }

            return true;
        }
    }
}
