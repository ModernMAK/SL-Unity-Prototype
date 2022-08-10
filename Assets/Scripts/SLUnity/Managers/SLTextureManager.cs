using System;
using System.IO;
using OpenMetaverse;
using OpenMetaverse.Assets;
using SLUnity.Data;
using SLUnity.Events;
using SLUnity.Objects;
using SLUnity.Threading;
using UnityEngine;
using ImageMagick;
namespace SLUnity.Managers
{
    // public class TextureCache : IDictionary<Primitive,Texture>
// {
//     
//     private ThreadDictionary<UUID, Texture> _assets;
//
//     public TextureCache()
//     {
//         _assets = new Dictionary<UUID, Texture>();
//         _generated = new Dictionary<Primitive.ConstructionData, Texture>();
//     }
//
//     public IEnumerator<KeyValuePair<Primitive, Texture>> GetEnumerator()
//     {
//         throw new NotImplementedException();
//     }
//
//     IEnumerator IEnumerable.GetEnumerator()
//     {
//         return GetEnumerator();
//     }
//
//     public void Add(KeyValuePair<Primitive, Texture> item) => Set(item.Key, item.Value);
//
//     public void Clear()
//     {
//         throw new NotImplementedException();
//     }
//
//     public bool Contains(KeyValuePair<Primitive, Texture> item)
//     {
//         throw new NotImplementedException();
//     }
//
//     public void CopyTo(KeyValuePair<Primitive, Texture>[] array, int arrayIndex)
//     {
//         throw new NotImplementedException();
//     }
//
//     public bool Remove(KeyValuePair<Primitive, Texture> item) => throw new NotImplementedException();
//
//     public int Count => _assets.Count + _generated.Count;
//
//     public bool IsReadOnly => throw new NotImplementedException();
//
//     private Texture Get(Primitive primitive)
//     {
//         return TryGetValue(primitive, out var Texture) ? Texture : null;
//     }
//     private bool TryGet(Primitive primitive, out Texture Texture)
//     {
//         
//         switch (primitive.Type)
//         {
//             case PrimType.Texture:
//                 //DOWNLOAD
//                 var assetKey = primitive.Sculpt.SculptTexture;
//                 return _assets.TryGetValue(assetKey, out Texture);
//                 break;
//             case PrimType.Sculpt:
//                 throw new NotSupportedException("Sculpted Texture is currently not supported");
//             case PrimType.Unknown:
//                 throw new InvalidOperationException();
//             case PrimType.Box:
//             case PrimType.Cylinder:
//             case PrimType.Prism:
//             case PrimType.Sphere:
//             case PrimType.Torus:
//             case PrimType.Tube:
//             case PrimType.Ring:
//                 //GENERATE
//                 var genKey = primitive.PrimData;
//                 return _generated.TryGetValue(genKey, out Texture);
//                 break;
//             default:
//                 throw new ArgumentOutOfRangeException();
//         }
//     }
//     private void Set(Primitive primitive, Texture Texture)
//     {
//         switch (primitive.Type)
//         {
//             case PrimType.Texture:
//                 //DOWNLOAD
//                 var assetKey = primitive.Sculpt.SculptTexture;
//                 _assets[assetKey] = Texture;
//                 break;
//             case PrimType.Sculpt:
//                 throw new NotSupportedException("Sculpted Texture is currently not supported");
//             case PrimType.Unknown:
//                 throw new InvalidOperationException();
//             case PrimType.Box:
//             case PrimType.Cylinder:
//             case PrimType.Prism:
//             case PrimType.Sphere:
//             case PrimType.Torus:
//             case PrimType.Tube:
//             case PrimType.Ring:
//                 //GENERATE
//                 var genKey = primitive.PrimData;
//                 _generated[genKey] = Texture;
//                 break;
//             default:
//                 throw new ArgumentOutOfRangeException();
//         }
//     }
//
//     public void Add(Primitive key, Texture value) => Set(key, value);
//
//     public bool ContainsKey(Primitive key)=> TryGet(key, out _);
//
//     public bool Remove(Primitive key)
//     {
//         throw new NotImplementedException();
//     }
//
//     public bool TryGetValue(Primitive key, out Texture value) => TryGet(key, out value);
//
//     public Texture this[Primitive key]
//     {
//         get => TryGet(key, out var Texture) ? Texture : throw new KeyNotFoundException();
//         set => Set(key, value);
//     }
//
//     public ICollection<Primitive> Keys =>  throw new NotImplementedException();
//
//     public ICollection<Texture> Values => throw new NotImplementedException();
// }

[RequireComponent(typeof(SLPrimitiveManager))]
    public class SLTextureManager : SLBehaviour
    {
        // private const int MAX_REQUESTS = 16;
        private ThreadVar<int> _requestCount;
        private ThreadDictionary<UUID, Texture> _cache;

        private ThreadQueue<UUID> _requestQueue;
        private ThreadDictionary<UUID, ThreadList<Action<Texture>>> _callbacks; //THIS IS A PRETTY GARBAGE HACK! 
    
        private TextureDiskCache _diskCache;
        private AssetBundleDiskCache<UUID, Texture> _assetCache;


        [Min(1)]
        public int MAX_REQUESTS = 1;
        private void Update()
        {
            //One Request Per Frame
            for (var i = 0; i < MAX_REQUESTS; i++)
            {
                if (_requestQueue.Count <= 0) return;
                var item = _requestQueue.Dequeue();
                StartRequest(item);
            }
        }

        private void Awake()
        {
            _diskCache = new TextureDiskCache();
            _requestCount = new ThreadVar<int>();
            _requestQueue = new ThreadQueue<UUID>();
            _callbacks = new ThreadDictionary<UUID, ThreadList<Action<Texture>>>();
            _cache = new ThreadDictionary<UUID, Texture>();

            _assetCache = new AssetBundleDiskCache<UUID, Texture>(
                "SLProtoAssets/Texture", 
                (uuid) => uuid.ToString(),
                (uuid) => uuid.ToString()
            );
            TextureCreated += TryCallback;
        }

        private void TryCallback(object sender, TextureCreatedArgs e)
        {
            if (!_callbacks.TryGetValue(e.Id, out var callbacks)) return;
            foreach(var callback in callbacks)
                callback(e.Texture);
            _callbacks[e.Id].Clear();
        }

        public void RequestTexture(UUID id, Action<Texture> callback)
        {
            if (_cache.TryGetValue(id, out var texture))
                callback(texture);
            else
            {
                if(_callbacks.TryGetValue(id, out var callbacks))
                    callbacks.Add(callback);
                else
                    _callbacks[id] = new ThreadList<Action<Texture>>(){callback};

                // if (_requestCount.Synchronized < MAX_REQUESTS)
                // {
                //     StartRequest(id);
                // }
                // else
                // {
                    _requestQueue.Enqueue(id);
                // }
                
            }
        }

        private void StartRequest(UUID id)
        {
            // _requestCount.Synchronized += 1;
            Manager.Threading.Unity.Global.Enqueue(() => TryLoadAssetBundle(id));

        }


        private void TryLoadAssetBundle(UUID id)
        {
            if (!_assetCache.Load(id, out var texture))
            {
                Manager.Threading.IO.Global.Enqueue(() => TryLoadTexture(id));
            }
            else
            {
                FinalizeTexture(id, texture);
            }
        }
        private void TryLoadTexture(UUID id)
        {
            if (!_diskCache.Load(id, out var tex))
            {
                // Manager.Threading.Data.Global.Enqueue(() => DownloadTexture(id));
                Manager.Threading.Unity.Global.Enqueue(() => DownloadTextureCoroutine(id));
            }
            else
            {
                Manager.Threading.Unity.Global.Enqueue(() => CreateTexture(id,tex));
            }
        }

        public event EventHandler<TextureCreatedArgs> TextureCreated;

        protected virtual void OnUnityTextureUpdated(TextureCreatedArgs e)
        {
            TextureCreated?.Invoke(this, e);
        }
        private void DownloadTexture(UUID texture)
        {
            Manager.Client.Assets.RequestImage(texture, ImageType.Normal, GetDownloadCallback(texture,0));
        }

        private void DownloadTextureCoroutine(UUID texture)
        {
            void Callback(AssetTexture assetTexture)
            {
                Manager.Threading.Data.Global.Enqueue(() => ConvertTexture(assetTexture.AssetID, assetTexture));
            }

            var coroutine = DownloadManager.DownloadTexture(texture, Callback);
            Manager.StartCoroutine(coroutine);
        }

        private TextureDownloadCallback GetDownloadCallback(UUID textureId, int tries)
        {
            const int MAX_TRIES = 3;
            void TextureDownloaded(TextureRequestState state, AssetTexture assetTexture)
            {
                switch (state)
                {
                    case TextureRequestState.Finished:
                        Manager.Threading.Data.Global.Enqueue(() => ConvertTexture(assetTexture.AssetID, assetTexture));
                        break;
                    case TextureRequestState.NotFound when tries >= MAX_TRIES:
                        throw new Exception("Texture download not found!");
                    case TextureRequestState.NotFound:
                        Manager.Client.Assets.RequestImage(textureId, ImageType.Normal, GetDownloadCallback(textureId,tries+1));
                        break;
                    case TextureRequestState.Timeout when tries >= MAX_TRIES:
                        throw new Exception("Texture download timed out!");
                    case TextureRequestState.Timeout:
                        Manager.Client.Assets.RequestImage(textureId, ImageType.Normal, GetDownloadCallback(textureId,tries+1));
                        break;
                }
            }

            return TextureDownloaded;
        }
    
        // private void ConvertTexture(UUID id, AssetTexture slTexture)
        // {
        //     var uTexture = UTexture.FromSL(slTexture);
        //     Manager.Threading.Unity.Global.Enqueue(() => CreateTexture(id,uTexture));
        //     Manager.Threading.Data.Global.Enqueue(()=> _diskCache.Store(id,uTexture));
        //
        // }
        private UTexture CreateUTex(byte[] jpeg2000)
        {
            using (var image = new MagickImage(jpeg2000))
            {
                var hasAlpha = image.HasAlpha;
                var format = hasAlpha ? MagickFormat.Dxt5 : MagickFormat.Dxt1;
                image.Format = format;
                var dxt = image.ToByteArray();
                var w = image.Width;
                var h = image.Height;
                return new UTexture(w, h, dxt, hasAlpha);
            }
        }

        private void ConvertTexture(UUID id, AssetTexture slTexture)
        {
            var utex = CreateUTex(slTexture.AssetData);
            Manager.Threading.Unity.Global.Enqueue(() => CreateTexture(id,utex));
            Manager.Threading.Data.Global.Enqueue(()=> _diskCache.Store(id,utex));

        }

        private void CreateTexture(UUID id, UTexture uTexture)
        {
            var texture = uTexture.ToUnity();
            FinalizeTexture(id, texture);
            // Manager.Threading.Unity.Global.Enqueue(() => CompressTexture(id, texture));
        }

        // private void CompressTexture(UUID id, Texture2D texture)
        // {
        //     texture.Compress(true);
        //     FinalizeTexture(id, texture);
        // }

        private void FinalizeTexture(UUID id, Texture texture)
        {
            // _requestCount.Synchronized -= 1;
            // while (_requestQueue.Count > 0 && _requestCount.Synchronized < MAX_REQUESTS)
            // {
            //     var item = _requestQueue.Dequeue();
            //     StartRequest(item);
            // }
            OnUnityTextureUpdated(new TextureCreatedArgs(id, texture));
        }
    }
}