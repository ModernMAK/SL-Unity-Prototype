using System.IO;
using SLUnity.Serialization;

namespace SLUnity.Snapshot
{
    public class SLRegionSerializer : ISerializer<SLRegionScene>
    {
        
        private PrimitiveSerializer _primitiveSerializer;

        public SLRegionSerializer()
        {
            _primitiveSerializer = new PrimitiveSerializer();
        }

        public void Write(BinaryWriter writer, SLRegionScene value)
        {
            writer.WriteArray(value.Primitives,_primitiveSerializer.Write);
            writer.WriteArray2D(value.Terrain,writer.Write);
        }

        public SLRegionScene Read(BinaryReader reader)
        {
            var primitives =reader.ReadArray(_primitiveSerializer.Read);
            var terrain = reader.ReadArray2D(reader.ReadSingle);
            return new SLRegionScene(primitives, terrain);
        }
    }
}