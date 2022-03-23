using System.Collections.Generic;
using System.IO;
using Coz.NET.Profiler.Experiment;
using ProtoBuf;

namespace Coz.NET.Profiler.Profile
{
    [ProtoContract]
    public class MethodMeasurement : IProtoSerializable
    {
        [ProtoMember(1)]
        public string MethodId { get; set; }

        [ProtoMember(2)]
        public List<long> Latencies { get; set; } 

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
            MethodMeasurement instance = Serializer.DeserializeWithLengthPrefix<MethodMeasurement>(stream, PrefixStyle.Fixed32);
            MethodId = instance.MethodId;
            Latencies = instance.Latencies; 
        }
    }
}