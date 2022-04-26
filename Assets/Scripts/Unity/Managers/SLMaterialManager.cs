// using System;
// using System.Collections;
// using System.Collections.Generic;
// using OpenMetaverse;
// using OpenMetaverse.Assets;
// using OpenMetaverse.Rendering;
// using UnityEngine;
// using Material = UnityEngine.Material;
// using Object = System.Object;
//
// // public class MaterialCache : IDictionary<Primitive,Material>
// // {
// //     private Dictionary<Primitive.ConstructionData, Material> _generated;
// //     private Dictionary<UUID, Material> _assets;
// //
// //     public MaterialCache()
// //     {
// //         _assets = new Dictionary<UUID, Material>();
// //         _generated = new Dictionary<Primitive.ConstructionData, Material>();
// //     }
// //
// //     public IEnumerator<KeyValuePair<Primitive, Material>> GetEnumerator()
// //     {
// //         throw new NotImplementedException();
// //     }
// //
// //     IEnumerator IEnumerable.GetEnumerator()
// //     {
// //         return GetEnumerator();
// //     }
// //
// //     public void Add(KeyValuePair<Primitive, Material> item) => Set(item.Key, item.Value);
// //
// //     public void Clear()
// //     {
// //         throw new NotImplementedException();
// //     }
// //
// //     public bool Contains(KeyValuePair<Primitive, Material> item)
// //     {
// //         throw new NotImplementedException();
// //     }
// //
// //     public void CopyTo(KeyValuePair<Primitive, Material>[] array, int arrayIndex)
// //     {
// //         throw new NotImplementedException();
// //     }
// //
// //     public bool Remove(KeyValuePair<Primitive, Material> item) => throw new NotImplementedException();
// //
// //     public int Count => _assets.Count + _generated.Count;
// //
// //     public bool IsReadOnly => throw new NotImplementedException();
// //
// //     private Material Get(Primitive primitive)
// //     {
// //         return TryGetValue(primitive, out var Material) ? Material : null;
// //     }
// //     private bool TryGet(Primitive primitive, out Material Material)
// //     {
// //         
// //         switch (primitive.Type)
// //         {
// //             case PrimType.Material:
// //                 //DOWNLOAD
// //                 var assetKey = primitive.Sculpt.SculptMaterial;
// //                 return _assets.TryGetValue(assetKey, out Material);
// //                 break;
// //             case PrimType.Sculpt:
// //                 throw new NotSupportedException("Sculpted Material is currently not supported");
// //             case PrimType.Unknown:
// //                 throw new InvalidOperationException();
// //             case PrimType.Box:
// //             case PrimType.Cylinder:
// //             case PrimType.Prism:
// //             case PrimType.Sphere:
// //             case PrimType.Torus:
// //             case PrimType.Tube:
// //             case PrimType.Ring:
// //                 //GENERATE
// //                 var genKey = primitive.PrimData;
// //                 return _generated.TryGetValue(genKey, out Material);
// //                 break;
// //             default:
// //                 throw new ArgumentOutOfRangeException();
// //         }
// //     }
// //     private void Set(Primitive primitive, Material Material)
// //     {
// //         switch (primitive.Type)
// //         {
// //             case PrimType.Material:
// //                 //DOWNLOAD
// //                 var assetKey = primitive.Sculpt.SculptMaterial;
// //                 _assets[assetKey] = Material;
// //                 break;
// //             case PrimType.Sculpt:
// //                 throw new NotSupportedException("Sculpted Material is currently not supported");
// //             case PrimType.Unknown:
// //                 throw new InvalidOperationException();
// //             case PrimType.Box:
// //             case PrimType.Cylinder:
// //             case PrimType.Prism:
// //             case PrimType.Sphere:
// //             case PrimType.Torus:
// //             case PrimType.Tube:
// //             case PrimType.Ring:
// //                 //GENERATE
// //                 var genKey = primitive.PrimData;
// //                 _generated[genKey] = Material;
// //                 break;
// //             default:
// //                 throw new ArgumentOutOfRangeException();
// //         }
// //     }
// //
// //     public void Add(Primitive key, Material value) => Set(key, value);
// //
// //     public bool ContainsKey(Primitive key)=> TryGet(key, out _);
// //
// //     public bool Remove(Primitive key)
// //     {
// //         throw new NotImplementedException();
// //     }
// //
// //     public bool TryGetValue(Primitive key, out Material value) => TryGet(key, out value);
// //
// //     public Material this[Primitive key]
// //     {
// //         get => TryGet(key, out var Material) ? Material : throw new KeyNotFoundException();
// //         set => Set(key, value);
// //     }
// //
// //     public ICollection<Primitive> Keys =>  throw new NotImplementedException();
// //
// //     public ICollection<Material> Values => throw new NotImplementedException();
// // }
//
// [RequireComponent(typeof(SLObjectManager))]
// public class SLMaterialManager : SLBehaviour
// {
//     public int MaxDownloadsPerTick = 8;
//     public int MaxRequestsPerTick = 8;
//     private Dictionary<UUID, Material> _cache;
//     private Queue<Tuple<UUID,AssetMaterial>> _downloadedQueue;
//     private Queue<UUID> _requestQueue;
//     private object _downloadedQueueLock;
//     private object _requestQueueLock;
//     private Dictionary<UUID, Action<Material>> _callbacks; //THIS IS A PRETTY GARBAGE HACK! 
//
//
//     private void Awake()
//     {
//         _callbacks = new Dictionary<UUID, Action<Material>>();
//         _cache = new Dictionary<UUID, Material>();
//         _downloadedQueueLock = new object();
//         _requestQueueLock = new object();
//         lock (_downloadedQueueLock)
//         {
//             _downloadedQueue = new Queue<Tuple<UUID,AssetMaterial>>();
//             
//         }
//
//         lock (_requestQueueLock)
//         {
//             _requestQueue = new Queue<UUID>();
//         }
//         MaterialCreated += TryCallback;
//     }
//
//     private void TryCallback(object sender, MaterialCreatedArgs e)
//     {
//         if (!_callbacks.TryGetValue(e.Id, out var callback)) return;
//         callback(e.Material);
//         _callbacks.Remove(e.Id);
//     }
//
//     private void FixedUpdate()
//     {
//         UUID id;
//         lock (_downloadedQueueLock)
//         {
//             var downloads = 0;
//             while (_downloadedQueue.Count > 0 && downloads < MaxDownloadsPerTick)
//             {
//                 downloads++;
//                 var primMaterialTuple = _downloadedQueue.Dequeue();
//                 id = primMaterialTuple.Item1;
//                 var material = primMaterialTuple.Item2;
//                 CreateMaterial(id,material);
//             }
//         }
//
//         lock (_requestQueueLock)
//         {
//             var requests = 0;
//             while (_requestQueue.Count > 0 && requests < MaxRequestsPerTick)
//             {
//                 requests++;
//                 id = _requestQueue.Dequeue();
//                 DownloadMaterial(id);
//             }
//         }
//     }
//
//     public void RequestMaterial(UUID id, Action<Material> callback)
//     {
//         if (_cache.TryGetValue(id, out var material))
//             callback(material);
//         else
//         {
//             lock (_requestQueueLock)
//             {
//                 _requestQueue.Enqueue(id);
//                 _callbacks[id] = callback;
//             }
//         }
//     }
//     
//
//
//     public event EventHandler<MaterialCreatedArgs> MaterialCreated;
//
//     protected virtual void OnUnityMaterialUpdated(MaterialCreatedArgs e)
//     {
//         MaterialCreated?.Invoke(this, e);
//     }
//     private void DownloadMaterial(UUID material)
//     {
//         Manager.Client.Assets.RequestImage(material,MaterialDownloaded);
//     }
//
//     private void MaterialDownloaded(MaterialRequestState state, AssetMaterial assetMaterial)
//     {
//         lock (_downloadedQueueLock)
//         {
//             _downloadedQueue.Enqueue(new Tuple<UUID, AssetMaterial>(assetMaterial.AssetID,assetMaterial));
//         }
//     }
//     //
//     // private voidMaterialDownloaded(Primitive primitive)
//     // {
//     //     void InternalCallback(bool success, AssetMaterial assetMaterial)
//     //     {
//     //         assetMaterial.
//     //         if (success)
//     //         {
//     //             if (FacetedMaterial.TryDecodeFromAsset(primitive, assetMaterial, DetailLevel.Highest, out var slMaterial))
//     //             {
//     //                 Debug.Log("DEBUG Material: Downloaded!");
//     //                 lock (_downloadedQueueLock)
//     //                 {
//     //                     _downloadedQueue.Enqueue(new Tuple<Primitive, FacetedMaterial>(primitive, slMaterial)); // Have to build our Material on the main thread
//     //                 }
//     //             }
//     //             else
//     //             {
//     //                 //TODO debug
//     //                 Debug.Log("DEBUG Material:Linden Material decoding failed!");
//     //             }
//     //         }
//     //         else
//     //         {
//     //             //TODO debug
//     //             Debug.Log("DEBUG Material: Material download failed!");
//     //         }
//     //     }
//     //
//     //     return InternalCallback;
//     // }
//
//     private void CreateMaterial(UUID id, AssetMaterial slMaterial)
//     {
//         var uMaterial = slMaterial.ToUnity();
//         OnUnityMaterialUpdated(new MaterialCreatedArgs(id, uMaterial));
//     }
// }
