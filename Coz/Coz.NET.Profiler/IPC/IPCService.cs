using System.IO;
using Coz.NET.Profiler.Experiment;

namespace Coz.NET.Profiler.IPC
{
    public class IPCService
    {
        private const string SHARED_FILE = @"C:\Users\tamas\Documents\Coz.NET\data.txt";

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Send<T>(T message) where T : IProtoSerializable, new()
        {
            var data = message.Serialize();
            File.WriteAllBytes(SHARED_FILE, data);
        }

        public T Receive<T>() where T : IProtoSerializable, new()
        {
            var instance = new T();

            using (var stream = new FileStream(SHARED_FILE, FileMode.Open))
            {
                instance.Deserialize(stream);
            }

            return instance;
        }
    }
}