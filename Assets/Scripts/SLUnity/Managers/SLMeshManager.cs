using System;
using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;
using SLUnity.Data;
using SLUnity.Events;
using SLUnity.Objects;
using SLUnity.Rendering;
using SLUnity.Threading;
using UnityEngine;
using Mesh = UnityEngine.Mesh;

namespace SLUnity.Managers
{
    [RequireComponent(typeof(SLPrimitiveManager))]
    public class SLMeshManager : SLBehaviour
    {
        private readonly IMeshGenerator _meshGenerator = new MeshGenerator();
        
        // private const int MAX_REQUESTS = 16;
    
        private static readonly IRendering MeshGen = new MeshmerizerR();
        private MeshCache _cache;
        private ThreadDictionary<UUID, Action<Mesh>> _callbacks; //THIS IS A PRETTY GARBAGE HACK! 
        private ThreadSet<UUID> _promises;
        private MeshDiskCache _diskCache;
        private ThreadVar<int> _taskCounter;
        private Queue<Tuple<Primitive, Action<Mesh>>> _requestQueue;
        private AssetBundleDiskCache<UUID, Mesh> _assetCache;

        private void Awake()
        {
            _promises = new ThreadSet<UUID>();
            _taskCounter = new ThreadVar<int>();
            _requestQueue = new Queue<Tuple<Primitive, Action<Mesh>>>();
            _callbacks = new ThreadDictionary<UUID, Action<Mesh>>();
            _cache = new MeshCache();

            _assetCache = new AssetBundleDiskCache<UUID, Mesh>(
                "SLProtoAssets/Mesh", 
                (uuid) => uuid.ToString(),
                (uuid) => uuid.ToString());
            _diskCache = new MeshDiskCache();
            MeshCreated += TryCallback;
        }

        private void TryCallback(object sender, PrimMeshCreatedArgs e)
        {
            if (!_callbacks.TryGetValue(e.Owner.ID, out var callback)) return;
            callback(e.GeneratedMesh);
            _callbacks.Remove(e.Owner.ID);
        }

        [Min(-1)]
        public int MAX_REQUESTS = 1;
        private void Update()
        {
            if(MAX_REQUESTS >= 0)
            //One Request Per Frame
                for (var i = 0; i < MAX_REQUESTS; i++)
                {
                    if (_requestQueue.Count <= 0) return;
                    var item = _requestQueue.Dequeue();
                    StartRequest(item.Item1,item.Item2);
                }
            else
                while (_requestQueue.Count > 0)
                {
                    var item = _requestQueue.Dequeue();
                    StartRequest(item.Item1,item.Item2);
                }
        }

        // private void FixedUpdate()
        // {
        //     Primitive prim;
        //     lock (_downloadedQueueLock)
        //     {
        //         var downloads = 0;
        //         while (_downloadedQueue.Count > 0 && downloads < MaxDownloadsPerTick)
        //         {
        //             downloads++;
        //             var primMeshTuple = _downloadedQueue.Dequeue();
        //             prim = primMeshTuple.Item1;
        //             var mesh = primMeshTuple.Item2;
        //             CreateMesh(prim,mesh);
        //         }
        //     }
        //
        //     lock (_requestQueueLock)
        //     {
        //         var requests = 0;
        //         while (_requestQueue.Count > 0 && requests < MaxRequestsPerTick)
        //         {
        //             requests++;
        //             prim = _requestQueue.Dequeue();
        //             switch (prim.Type)
        //             {
        //                 case PrimType.Mesh:
        //                     DownloadMesh(prim);
        //                     break;
        //                 case PrimType.Sculpt:
        //                 case PrimType.Unknown:
        //                     return;
        //                 case PrimType.Box:
        //                 case PrimType.Cylinder:
        //                 case PrimType.Prism:
        //                 case PrimType.Sphere:
        //                 case PrimType.Torus:
        //                 case PrimType.Tube:
        //                 case PrimType.Ring:
        //                     GenerateMesh(prim);
        //                     break;
        //                 default:
        //                     throw new ArgumentOutOfRangeException(nameof(prim.Type));
        //             }
        //         }
        //     }
        // }

        public void RequestMesh(Primitive primitive, Action<Mesh> callback)
        {
            if (_cache.TryGetValue(primitive, out var cacheMesh))
                callback(cacheMesh);
            else
            {
                QueueRequest(primitive,callback);
            }
        }

        private void QueueRequest(Primitive primitive, Action<Mesh> callback)
        {
            // if(_requestQueue.Count < MAX_REQUESTS)
            //     StartRequest(primitive,callback);
            // else
            _requestQueue.Enqueue(new Tuple<Primitive, Action<Mesh>>(primitive,callback));
        }

        private void StartRequest(Primitive primitive, Action<Mesh> callback)
        {            
            _taskCounter.Synchronized += 1;
            _callbacks[primitive.ID] = callback;
            switch (primitive.Type)
            {
                case PrimType.Unknown:
                    break;
                case PrimType.Box:
                case PrimType.Cylinder:
                case PrimType.Prism:
                case PrimType.Sphere:
                case PrimType.Torus:
                case PrimType.Tube:
                case PrimType.Ring:
                    Manager.Threading.Data.Global.Enqueue(() => GenerateMesh(primitive));
                    break;
                case PrimType.Sculpt:
                    _taskCounter.Synchronized -= 1;
                    break;
                    throw new NotSupportedException();
                case PrimType.Mesh:
                    Manager.Threading.Unity.Global.Enqueue(() => TryLoadAssetBundle(primitive));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void FinishRequest()
        {
            // _taskCounter.Synchronized -= 1;
            // while (_taskCounter.Synchronized < MAX_REQUESTS && _requestQueue.Count > 0)
            // {
            //     var item = _requestQueue.Dequeue();
            //     StartRequest(item.Item1,item.Item2);
            // }
        }


        public event EventHandler<PrimMeshCreatedArgs> MeshCreated;

        protected virtual void OnUnityMeshUpdated(PrimMeshCreatedArgs e)
        {
            MeshCreated?.Invoke(this, e);
        }

        private void TryLoadAssetBundle(Primitive primitive)
        {
            if (!_assetCache.Load(primitive.Sculpt.SculptTexture, out var mesh))
            {
                Manager.Threading.IO.Global.Enqueue(() => TryLoadFromDisk(primitive));
            }
            else
            {
                FinalizeMesh(primitive, mesh);
            }
        }
        private void TryLoadFromDisk(Primitive primitive)
        {
            if (_diskCache.Load(primitive.Sculpt.SculptTexture, out var meshData))
            {
                Manager.Threading.Unity.Global.Enqueue(()=> CreateMesh(primitive,meshData));
            }
            else
            {
                Manager.Threading.IO.Global.Enqueue(()=> DownloadMeshCoroutine(primitive));
                // Manager.Threading.Data.Global.Enqueue(() => DownloadMesh(primitive));
            }
        }
        private void DownloadMesh(Primitive primitive)
        {
            Manager.Client.Assets.RequestMesh(primitive.Sculpt.SculptTexture,MeshDownloaded(primitive));
            
        }

        private void DownloadMeshCoroutine(Primitive primitive)
        {
            
            void GenMesh(AssetMesh assetMesh)
            {
                Debug.Log("Decoding Mesh From Downloaded Data");
                // var meshData = _meshGenerator.GenerateMeshData(assetMesh);
                if (FacetedMesh.TryDecodeFromAsset(primitive, assetMesh, DetailLevel.Highest, out var slMesh))
                {
                    // Debug.Log("DEBUG MESH: Downloaded!");
                    var meshData = UMeshData.FromSL(slMesh);
                    Manager.Threading.Data.Global.Enqueue(()=>CacheMesh(primitive,meshData));
                    Manager.Threading.Unity.Global.Enqueue(()=>CreateMesh(primitive,meshData));
                }
                else
                {
                    //TODO debug
                    Debug.Log("DEBUG MESH:Linden Mesh decoding failed!");
                }
            }

            void Callback(AssetMesh assetMesh) => Manager.Threading.Data.Global.Enqueue(() => GenMesh(assetMesh));
            Manager.Downloader.DownloadMesh(primitive.Sculpt.SculptTexture, Callback);
        }

        private AssetManager.MeshDownloadCallback MeshDownloaded(Primitive primitive)
        {
            void InternalCallback(bool success, AssetMesh assetMesh)
            {
            
                void DataThreadCallback()
                {
                    Debug.Log("Decoding Mesh From Downloaded Data");
                    // var meshData = _meshGenerator.GenerateMeshData(assetMesh);
                    if (FacetedMesh.TryDecodeFromAsset(primitive, assetMesh, DetailLevel.Highest, out var slMesh))
                    {
                    // Debug.Log("DEBUG MESH: Downloaded!");
                        var meshData = UMeshData.FromSL(slMesh);
                        Manager.Threading.Data.Global.Enqueue(()=>CacheMesh(primitive,meshData));
                        Manager.Threading.Unity.Global.Enqueue(()=>CreateMesh(primitive,meshData));
                    }
                    else
                    {
                        //TODO debug
                        Debug.Log("DEBUG MESH:Linden Mesh decoding failed!");
                    }
                }
            
                if (success)
                {
                    Debug.Log("Downloaded Mesh");
                    Manager.Threading.Data.Global.Enqueue(DataThreadCallback);
                }
                else
                {
                    //TODO debug
                    Debug.Log("DEBUG MESH: Mesh download failed!");
                }
            }

            return InternalCallback;
        }

        private void CacheMesh(Primitive primitive, UMeshData uMeshData) =>
            _diskCache.Store(primitive.Sculpt.SculptTexture, uMeshData);

        private void GenerateMesh(Primitive primitive)
        {
            void Internal()
            {
                Debug.Log("Generating Mesh From Construction Data");
                var slMesh = MeshGen.GenerateFacetedMesh(primitive, DetailLevel.Highest);
                var uMesh = UMeshData.FromSL(slMesh);
                Manager.Threading.Unity.Global.Enqueue(() => CreateMesh(primitive, uMesh));
            }
            Manager.Threading.Data.Global.Enqueue(Internal);

        }
        private void CreateMesh(Primitive primitive, UMeshData uMesh)
        {
            Debug.Log("Creating Mesh From UMeshData");
            var m = uMesh.ToUnity();
            m.name = primitive.Type == PrimType.Mesh ? primitive.Sculpt.SculptTexture.ToString() : "Generated";
            // _cache[primitive] = m;
            // OnUnityMeshUpdated(new PrimMeshCreatedArgs(primitive, m));
            Manager.Threading.Unity.Global.Enqueue(() => OptimizeMesh(primitive,m));
        }

        private void OptimizeMesh(Primitive primitive, Mesh mesh)
        {
            mesh.Optimize();
            FinalizeMesh(primitive, mesh);
        }

        private void FinalizeMesh(Primitive primitive, Mesh mesh)
        {
            _cache[primitive] = mesh;
            FinishRequest();
            OnUnityMeshUpdated(new PrimMeshCreatedArgs(primitive, mesh));
        }
    }
}