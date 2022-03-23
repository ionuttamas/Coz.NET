using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Coz.NET.Profiler.Experiment;

namespace Coz.NET.Profiler.IPC
{
    public class IPCService
    {
        private const long MAX_MESSAGE_CAPACITY = 1000000;
        private const string MEMORY_MAPPED_FILE = "COZ_MMF";
        private MemoryMappedFile channel;

        public void Open()
        {
            channel = MemoryMappedFile.CreateOrOpen(MEMORY_MAPPED_FILE, MAX_MESSAGE_CAPACITY);
        }

        public void Close()
        {
            channel.Dispose();
        }

        public void Send<T>(T message) where T : IProtoSerializable, new()
        {
            using (var stream = channel.CreateViewStream())
            {
                var data = message.Serialize();

                if (data.Length > MAX_MESSAGE_CAPACITY)
                    throw new InvalidOperationException(
                        $"Transmitted message {message} exceeds limit of {MAX_MESSAGE_CAPACITY} bytes");

                stream.Seek(0, SeekOrigin.Begin);
                stream.Write(data);
                stream.Flush();
            }
        }

        public T Receive<T>() where T : IProtoSerializable, new()
        {
            var instance = new T();

            using (var stream = channel.CreateViewStream())
            {
                instance.Deserialize(stream);
                stream.Seek(0, SeekOrigin.Begin);
            }

            return instance;
        }
    }
}