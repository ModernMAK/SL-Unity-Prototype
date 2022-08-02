using System.IO;

namespace Unity.Managers
{
    public interface ISerializer<T>
    {
        void Write(BinaryWriter writer, T value);
        T Read(BinaryReader reader);
    }
}