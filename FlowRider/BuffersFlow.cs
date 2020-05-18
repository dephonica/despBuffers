using System;
using System.Collections.Generic;
using System.Linq;
using despBuffers.Interfaces;

namespace despBuffers.FlowRider
{
    public class BuffersFlow
    {
        public string FlowName { get; set; }
        public static Guid InfoGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");

        private static BuffersFlow _instance;

        public static BuffersFlow Instance =>
            FlowRiderSettings.IsFlowRecorded
                ? _instance ?? (_instance = new BuffersFlow())
                : null;

        private readonly Dictionary<Guid, string> _flowBufferNames = new Dictionary<Guid, string>();

        private readonly Dictionary<Guid, List<BufferFlowDescription>> _flowDictionary =
            new Dictionary<Guid, List<BufferFlowDescription>>();

        public BuffersFlow()
        {
            var timestamp = DateTime.Now;
            FlowName =
                $"buffersflow_{timestamp.Year}-{timestamp.Month}-{timestamp.Day}_{timestamp.Hour}-{timestamp.Minute}-{timestamp.Second}";
        }

        public void PostInfo(int id, string infoText)
        {
            if (FlowRiderSettings.IsFlowRecorded == false)
            {
                return;
            }

            lock (_flowDictionary)
            {
                var uid = InfoGuid;
                var infoEvent = new BufferFlowDescription {InfoId = id, Info = infoText};

                if (_flowDictionary.ContainsKey(uid) == false)
                {
                    _flowDictionary.Add(uid, new List<BufferFlowDescription>());
                    _flowBufferNames.Add(uid, "Info flow");
                }

                var flowList = _flowDictionary[uid];
                flowList.Add(infoEvent);
            }
        }

        public void OnBufferChanged(IAudioBufferProto eventSource)
        {
            if (FlowRiderSettings.IsFlowRecorded == false)
            {
                return;
            }

            lock (_flowDictionary)
            {
                if (_flowDictionary.ContainsKey(eventSource.BufferUid) == false)
                {
                    _flowDictionary.Add(eventSource.BufferUid, new List<BufferFlowDescription>());

                    if (string.IsNullOrEmpty(eventSource.Name))
                    {
                        throw new BuffersException(
                            $"Unable to register flow for unnamed buffer with UID {eventSource.BufferUid}");
                    }

                    _flowBufferNames.Add(eventSource.BufferUid, eventSource.Name);
                }

                var flowList = _flowDictionary[eventSource.BufferUid];

                var lastState = flowList.LastOrDefault();
                var currentState = BufferFlowDescription.FromSource(eventSource);

                if (lastState == null && currentState.DataLength == 0)
                {
                    return;
                }

                flowList.Add(currentState);
            }
        }

        public void Flush()
        {
            if (FlowRiderSettings.IsFlowRecorded == false)
            {
                return;
            }

            FlowSerialized.FromSource(_flowDictionary, _flowBufferNames).ToFile($"{FlowName}.bin");
        }
    }
}
