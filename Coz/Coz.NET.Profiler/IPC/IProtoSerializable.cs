using System.IO;

namespace Coz.NET.Profiler.Experiment
{
    public interface IProtoSerializable
    {
        byte[] Serialize();
        void Deserialize(Stream stream);
    }
}