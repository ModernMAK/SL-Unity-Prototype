using OpenMetaverse;

namespace SLUnity.Snapshot
{
    public class SLRegionScene
    {
        public readonly Primitive[] Primitives;
        public readonly float[,] Terrain;

        public SLRegionScene(Primitive[] primitives, float[,] terrain)
        {
            Primitives = primitives;
            Terrain = terrain;
        }
    }
}