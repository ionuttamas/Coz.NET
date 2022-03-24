using System.Collections.Generic;
using System.IO;
using Coz.NET.Profiler.Experiment;
using ProtoBuf;

namespace Coz.NET.Profiler.Marker
{
    [ProtoContract]
    public class CozSnapshot : IProtoSerializable
    {
        [ProtoMember(1)]
        public string ExperimentId { get; set; }

        [ProtoMember(2)]
        public List<string> LatencyTags { get; set; }

        [ProtoMember(3)]
        public List<long> Latencies { get; set; }

        [ProtoMember(4)]
        public List<string> ThroughputTags { get; set; }

        [ProtoMember(5)]
        public List<double> Throughputs { get; set; }

        public byte[] Serialize()
        {
            byte[] data;

            using (var stream = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix(stream, this, PrefixStyle.Fixed32);
                data = stream.ToArray();
            }   

            return data;
        }

        public void Deserialize(Stream stream)
        {
            CozSnapshot instance = Serializer.DeserializeWithLengthPrefix<CozSnapshot>(stream, PrefixStyle.Fixed32);
            ExperimentId = instance.ExperimentId;
            LatencyTags = instance.LatencyTags;
            Latencies = instance.Latencies;
            ThroughputTags = instance.ThroughputTags;
            Throughputs = instance.Throughputs;
        }
    }
}