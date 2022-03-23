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
        public string Latencies { get; set; }

        [ProtoMember(3)]
        public string Throughput { get; set; }

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
            Latencies = instance.Latencies;
            Throughput = instance.Throughput;
        }
    }
}