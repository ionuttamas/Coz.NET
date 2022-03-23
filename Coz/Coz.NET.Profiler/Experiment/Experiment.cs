using System.Collections.Generic;
using System.IO;
using ProtoBuf;

namespace Coz.NET.Profiler.Experiment
{
    [ProtoContract]
    public class Experiment : IProtoSerializable
    {
        public Experiment() { }

        public Experiment(string id, string methodId, int methodSlowdown)
        {
            Id = id;
            //At the moment we only have millisecond thread sleep granularity
            MethodId = methodId;
            MethodSlowdown = methodSlowdown;
        }

        [ProtoMember(1)]
        public string Id { get; set; }
        
        [ProtoMember(2)]
        public string MethodId { get; set; }

        [ProtoMember(3)]
        public int MethodSlowdown { get; set; }

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
            Experiment instance = Serializer.DeserializeWithLengthPrefix<Experiment>(stream, PrefixStyle.Fixed32);
            MethodId = instance.MethodId;
            MethodSlowdown = instance.MethodSlowdown;
        }
    }
}