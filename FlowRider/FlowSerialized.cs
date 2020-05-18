using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace despBuffers.FlowRider
{
    [Serializable]
    public class FlowSerialized
    {
        public Type FlowItemType { get; }

        public object FlowObject { get; }
        public Dictionary<Guid, string> FlowNames { get; }

        private FlowSerialized(object flowSource, Dictionary<Guid, string> flowNames)
        {
            FlowObject = flowSource;
            FlowItemType = flowSource.GetType();
            FlowNames = flowNames;
        }

        public static FlowSerialized FromSource(object flowSource, Dictionary<Guid, string> flowNames)
        {
            return new FlowSerialized(flowSource, flowNames);
        }

        public void ToFile(string fileName)
        {
            var formatter = new BinaryFormatter();

            using (var storeStream = new MemoryStream())
            {
                formatter.Serialize(storeStream, this);

                File.WriteAllBytes(fileName, storeStream.ToArray());
            }
        }

        public static FlowSerialized FromFile(string fileName)
        {
            var formatter = new BinaryFormatter();

            using (var inputMemoryStream = new MemoryStream(File.ReadAllBytes(fileName)))
            {
                var flowObject = formatter.Deserialize(inputMemoryStream);

                return (FlowSerialized) flowObject;
            }
        }
    }
}
