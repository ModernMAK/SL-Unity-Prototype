using System.IO;

namespace SLUnity.Serialization
{
    public interface ISerializer<T>
    {
        void Write(BinaryWriter writer, T value);
        T Read(BinaryReader reader);
    }
}