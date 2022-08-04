using OpenMetaverse;
using SLUnity.Data;

namespace SLUnity.Managers
{
    public class MeshDiskCache : DiskCacheThreadable<UUID,UMeshData>
    {
        public const string DefaultCacheLocation = "SLProtoCache/Mesh";
        public static string DefaultPathFunc(UUID id) => $"{id}.umesh";

        public MeshDiskCache() : base(DefaultCacheLocation,DefaultPathFunc,new UMeshData.Serializer())
        {
        }
    }
}