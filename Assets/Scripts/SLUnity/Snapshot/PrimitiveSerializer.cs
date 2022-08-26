using System.Data;
using System.IO;
using OpenMetaverse;
using OpenMetaverse.ImportExport.Collada14;
using SLUnity.Serialization;

namespace SLUnity.Snapshot
{
    public class PrimitiveSerializer : ISerializer<Primitive>
    {
        private readonly OSDSerializer _osdSerializer;

        public PrimitiveSerializer()
        {
            _osdSerializer = new OSDSerializer();
        }

        public void Write(BinaryWriter writer, Primitive value) => _osdSerializer.Write(writer,value.GetOSD());


        public Primitive Read(BinaryReader reader) => Primitive.FromOSD(_osdSerializer.Read(reader));
    }
}