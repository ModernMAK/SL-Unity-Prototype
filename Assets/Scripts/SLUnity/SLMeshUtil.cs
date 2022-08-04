using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;

namespace SLUnity
{
    public static class SLMeshUtil
    {
        private static readonly DetailLevel[] DetailLevels = new []
            { DetailLevel.Highest, DetailLevel.High, DetailLevel.Medium, DetailLevel.Low };

        public static IEnumerable<DetailLevel> DetailHi2Lo
        {
            get
            {
                for (var i = 0; i < DetailLevels.Length; i++)
                    yield return DetailLevels[i];
            }
        }
        public static IEnumerable<DetailLevel> DetailLo2Hi
        {
            get
            {
                for (var i = DetailLevels.Length-1; i >= 0; i--)
                    yield return DetailLevels[i];
            }
        }

        public static bool TryDecodeHighestLOD(Primitive prim, AssetMesh assetMesh, out DetailLevel LOD, out FacetedMesh mesh)
        {
            foreach (var lod in DetailHi2Lo)
            {
                if (!FacetedMesh.TryDecodeFromAsset(prim, assetMesh, lod, out mesh)) continue;
                LOD = lod;
                return true;
            }

            LOD = default;
            mesh = default;
            return false;

        }
    }
}