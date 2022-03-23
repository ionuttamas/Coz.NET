using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using ProtoBuf;

namespace TestApp
{
    [ProtoContract]
    public class MethodSlowdown
    {
        public MethodSlowdown() { }

        public MethodSlowdown(string filePath, string methodName, long lineNumber, long slowdown)
        {
            FilePath = filePath;
            MethodName = methodName;
            LineNumber = lineNumber;
            Slowdown = slowdown;
        }

        [ProtoMember(1)]
        public string FilePath { get; set; }
        [ProtoMember(2)]
        public string MethodName { get; set; }
        [ProtoMember(3)]
        public long LineNumber { get; set; }
        [ProtoMember(4)]
        public long Slowdown { get; set; }

        public byte[] Serialize()
        {
            byte[] data;

            using (var ms = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix(ms, this, PrefixStyle.Fixed32);
                data = ms.ToArray();
            }

            return data;
        }

        public void Deserialize(Stream stream)
        {
            var instance = Serializer.DeserializeWithLengthPrefix<MethodSlowdown>(stream, PrefixStyle.Fixed32);
            FilePath = instance.FilePath;
            MethodName = instance.MethodName;
            LineNumber = instance.LineNumber;
            Slowdown = instance.Slowdown;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var length = 10000;
            var instance = new MethodSlowdown("fp", "mName", 65, 4325);

            var mmf = MemoryMappedFile.CreateNew(@"MMF", length);
            {
                using (var stream = mmf.CreateViewStream())
                {
                    stream.Write(instance.Serialize());
                    stream.Flush();
                }
            }

            var food = new Class1();
            food.Foo();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            long counter = long.MinValue;

            for (long i = long.MinValue; i < long.MaxValue; i++)
            {
                double a = 2+i*i;
            }

            stopwatch.Stop();

            Console.WriteLine(stopwatch.ElapsedMilliseconds);
            Console.Read();
        }
    }
}
