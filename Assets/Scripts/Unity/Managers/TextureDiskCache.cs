using System;
using OpenMetaverse;

namespace Unity.Managers
{
    public class TextureDiskCache : DiskCacheThreadable<UUID,UTexture>
    {
        public const string DefaultCacheLocation = "SLProtoCache/Texture";
        private string _cacheLocation;
        private Func<UUID, string> _pathFunc;

        public static string DefaultPathFunc(UUID id) => $"{id}.utex";

        public TextureDiskCache() : base(DefaultCacheLocation,DefaultPathFunc,new UTexture.Serializer())
        {
        }
    }
}