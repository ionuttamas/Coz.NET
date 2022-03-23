using System.IO;
using ProtoBuf;

namespace Coz.NET.Profiler.Experiment
{
    [ProtoContract]
    public class Experiment : IProtoSerializable
    {
        public Experiment() { }

        public Experiment(string id, string filePath, string methodName, long lineNumber, int slowdown)
        {
            Id = id;
            FilePath = filePath;
            MethodName = methodName;
            LineNumber = lineNumber;
            //At the moment we only have millisecond thread sleep granularity
            MethodSlowdown = slowdown;
        }

        [ProtoMember(1)]
        public string Id { get; set; }

        [ProtoMember(2)]
        public string FilePath { get; set; }
        
        [ProtoMember(3)]
        public string MethodName { get; set; }
        
        [ProtoMember(4)]
        public long LineNumber { get; set; }
        
        [ProtoMember(5)]
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
            FilePath = instance.FilePath;
            MethodName = instance.MethodName;
            LineNumber = instance.LineNumber;
            MethodSlowdown = instance.MethodSlowdown;
        }
    }
}