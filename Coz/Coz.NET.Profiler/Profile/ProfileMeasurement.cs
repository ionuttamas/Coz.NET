using System.Collections.Generic;
using System.IO;
using Coz.NET.Profiler.Experiment;
using Coz.NET.Profiler.Marker;
using ProtoBuf;

namespace Coz.NET.Profiler.Profile
{
    [ProtoContract]
    public class ProfileMeasurement : IProtoSerializable
    {
        [ProtoMember(1)]
        public List<MethodMeasurement> MethodMeasurements { get; set; }

        [ProtoMember(2)]
        public CozSnapshot CozSnapshot { get; set; }

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
            ProfileMeasurement instance = Serializer.DeserializeWithLengthPrefix<ProfileMeasurement>(stream, PrefixStyle.Fixed32);
            MethodMeasurements = instance.MethodMeasurements;
            CozSnapshot = instance.CozSnapshot;
        }
    }
}