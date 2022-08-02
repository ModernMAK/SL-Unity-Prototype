using OpenMetaverse;
using UnityTemplateProjects.Unity;

namespace Unity.Managers
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