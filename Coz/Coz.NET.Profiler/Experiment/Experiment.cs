using System.Collections.Generic;
using System.IO;
using ProtoBuf;

namespace Coz.NET.Profiler.Experiment
{
    [ProtoContract]
    public class Experiment : IProtoSerializable
    {
        public Experiment() { } 

        [ProtoMember(1)]
        public string Id { get; set; }
        
        [ProtoMember(2)]
        public string MethodId { get; set; }

        [ProtoMember(3)]
        public float MethodPercentageSlowdown { get; set; }

        [ProtoMember(4)]
        public int MethodSlowdown { get; set; }

        public bool IsBaseline => MethodSlowdown == 0;

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
            Id = instance.Id;
            MethodId = instance.MethodId;
            MethodSlowdown = instance.MethodSlowdown;
        }

        public override string ToString()
        {
            return $"[Id: {Id}] - [MethodId: {MethodId}] - [MethodPercentageSlowdown: {MethodPercentageSlowdown}] - [MethodSlowdown: {MethodSlowdown}]";
        }
    }
}