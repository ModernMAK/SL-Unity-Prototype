using System;
using System.Collections;
using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;
using UnityEngine;
using Texture = UnityEngine.Texture;
using Object = System.Object;

// public class TextureCache : IDictionary<Primitive,Texture>
// {
//     private Dictionary<Primitive.ConstructionData, Texture> _generated;
//     private Dictionary<UUID, Texture> _assets;
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

[RequireComponent(typeof(SLObjectManager))]
public class SLTextureManager : SLBehaviour
{
    public int MaxDownloadsPerTick = 8;
    public int MaxRequestsPerTick = 8;
    private Dictionary<UUID, Texture> _cache;
    private Queue<Tuple<UUID,AssetTexture>> _downloadedQueue;
    private Queue<UUID> _requestQueue;
    private object _downloadedQueueLock;
    private object _requestQueueLock;
    private Dictionary<UUID, Action<Texture>> _callbacks; //THIS IS A PRETTY GARBAGE HACK! 


    private void Awake()
    {
        _callbacks = new Dictionary<UUID, Action<Texture>>();
        _cache = new Dictionary<UUID, Texture>();
        _downloadedQueueLock = new object();
        _requestQueueLock = new object();
        lock (_downloadedQueueLock)
        {
            _downloadedQueue = new Queue<Tuple<UUID,AssetTexture>>();
            
        }

        lock (_requestQueueLock)
        {
            _requestQueue = new Queue<UUID>();
        }
        TextureCreated += TryCallback;
    }

    private void TryCallback(object sender, TextureCreatedArgs e)
    {
        if (!_callbacks.TryGetValue(e.Id, out var callback)) return;
        callback(e.Texture);
        _callbacks.Remove(e.Id);
    }

    private void FixedUpdate()
    {
        UUID id;
        lock (_downloadedQueueLock)
        {
            var downloads = 0;
            while (_downloadedQueue.Count > 0 && downloads < MaxDownloadsPerTick)
            {
                downloads++;
                var primTextureTuple = _downloadedQueue.Dequeue();
                id = primTextureTuple.Item1;
                var texture = primTextureTuple.Item2;
                CreateTexture(id,texture);
            }
        }

        lock (_requestQueueLock)
        {
            var requests = 0;
            while (_requestQueue.Count > 0 && requests < MaxRequestsPerTick)
            {
                requests++;
                id = _requestQueue.Dequeue();
                DownloadTexture(id);
            }
        }
    }

    public void RequestTexture(UUID id, Action<Texture> callback)
    {
        if (_cache.TryGetValue(id, out var texture))
            callback(texture);
        else
        {
            lock (_requestQueueLock)
            {
                _requestQueue.Enqueue(id);
                _callbacks[id] = callback;
            }
        }
    }
    


    public event EventHandler<TextureCreatedArgs> TextureCreated;

    protected virtual void OnUnityTextureUpdated(TextureCreatedArgs e)
    {
        TextureCreated?.Invoke(this, e);
    }
    private void DownloadTexture(UUID texture)
    {
        Manager.Client.Assets.RequestImage(texture,TextureDownloaded);
    }

    private void TextureDownloaded(TextureRequestState state, AssetTexture assetTexture)
    {
        lock (_downloadedQueueLock)
        {
            _downloadedQueue.Enqueue(new Tuple<UUID, AssetTexture>(assetTexture.AssetID,assetTexture));
        }
    }
    //
    // private voidTextureDownloaded(Primitive primitive)
    // {
    //     void InternalCallback(bool success, AssetTexture assetTexture)
    //     {
    //         assetTexture.
    //         if (success)
    //         {
    //             if (FacetedTexture.TryDecodeFromAsset(primitive, assetTexture, DetailLevel.Highest, out var slTexture))
    //             {
    //                 Debug.Log("DEBUG Texture: Downloaded!");
    //                 lock (_downloadedQueueLock)
    //                 {
    //                     _downloadedQueue.Enqueue(new Tuple<Primitive, FacetedTexture>(primitive, slTexture)); // Have to build our Texture on the main thread
    //                 }
    //             }
    //             else
    //             {
    //                 //TODO debug
    //                 Debug.Log("DEBUG Texture:Linden Texture decoding failed!");
    //             }
    //         }
    //         else
    //         {
    //             //TODO debug
    //             Debug.Log("DEBUG Texture: Texture download failed!");
    //         }
    //     }
    //
    //     return InternalCallback;
    // }

    private void CreateTexture(UUID id, AssetTexture slTexture)
    {
        var uTexture = slTexture.ToUnity();
        OnUnityTextureUpdated(new TextureCreatedArgs(id, uTexture));
    }
}
