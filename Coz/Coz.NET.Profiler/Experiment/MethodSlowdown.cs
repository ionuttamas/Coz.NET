using System.IO;
using ProtoBuf;

namespace Coz.NET.Profiler.Experiment
{
    [ProtoContract]
    public class MethodSlowdown : IProtoSerializable
    {
        public MethodSlowdown() { }

        public MethodSlowdown(string filePath, string methodName, long lineNumber, int slowdown)
        {
            FilePath = filePath;
            MethodName = methodName;
            LineNumber = lineNumber;
            //At the moment we only have millisecond thread sleep granularity
            Slowdown = slowdown;
        }

        [ProtoMember(1)]
        public string FilePath { get; set; }
        [ProtoMember(2)]
        public string MethodName { get; set; }
        [ProtoMember(3)]
        public long LineNumber { get; set; }
        [ProtoMember(4)]
        public int Slowdown { get; set; }

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
            MethodSlowdown instance = Serializer.DeserializeWithLengthPrefix<MethodSlowdown>(stream, PrefixStyle.Fixed32);
            FilePath = instance.FilePath;
            MethodName = instance.MethodName;
            LineNumber = instance.LineNumber;
            Slowdown = instance.Slowdown;
        }
    }
}