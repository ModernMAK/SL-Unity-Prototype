using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;
using UnityEngine;
using UnityTemplateProjects;
using UnityTemplateProjects.Unity;
using Mesh = UnityEngine.Mesh;
using Object = System.Object;

public class MeshCache : Threadable, IDictionary<Primitive,Mesh>
{
    private readonly Dictionary<Primitive.ConstructionData, Mesh> _generated;
    private readonly Dictionary<UUID, Mesh> _assets;

    public MeshCache()
    {
        _assets = new Dictionary<UUID, Mesh>();
        _generated = new Dictionary<Primitive.ConstructionData, Mesh>();
    }

    public IEnumerator<KeyValuePair<Primitive, Mesh>> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<Primitive, Mesh> item) => Set(item.Key, item.Value);

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(KeyValuePair<Primitive, Mesh> item)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(KeyValuePair<Primitive, Mesh>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public bool Remove(KeyValuePair<Primitive, Mesh> item) => throw new NotImplementedException();

    public int Count
    {
        get
        {
            lock (SyncRoot)
            {
                return _assets.Count + _generated.Count;
            }
        }
    } 

    public bool IsReadOnly => throw new NotImplementedException();

    private Mesh Get(Primitive primitive)
    {
        return TryGetValue(primitive, out var mesh) ? mesh : null;
    }
    private bool TryGet(Primitive primitive, out Mesh mesh)
    {
        lock(SyncRoot)
        {
            switch (primitive.Type)
            {
                case PrimType.Mesh:
                    //DOWNLOAD
                    var assetKey = primitive.Sculpt.SculptTexture;
                    return _assets.TryGetValue(assetKey, out mesh);
                    break;
                case PrimType.Sculpt:
                    throw new NotSupportedException("Sculpted mesh is currently not supported");
                case PrimType.Unknown:
                    throw new InvalidOperationException();
                case PrimType.Box:
                case PrimType.Cylinder:
                case PrimType.Prism:
                case PrimType.Sphere:
                case PrimType.Torus:
                case PrimType.Tube:
                case PrimType.Ring:
                    //GENERATE
                    var genKey = primitive.PrimData;
                    return _generated.TryGetValue(genKey, out mesh);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    private void Set(Primitive primitive, Mesh mesh)
    {
        lock(SyncRoot)
        {
            switch (primitive.Type)
            {
                case PrimType.Mesh:
                    //DOWNLOAD
                    var assetKey = primitive.Sculpt.SculptTexture;
                    _assets[assetKey] = mesh;
                    break;
                case PrimType.Sculpt:
                    throw new NotSupportedException("Sculpted mesh is currently not supported");
                case PrimType.Unknown:
                    throw new InvalidOperationException();
                case PrimType.Box:
                case PrimType.Cylinder:
                case PrimType.Prism:
                case PrimType.Sphere:
                case PrimType.Torus:
                case PrimType.Tube:
                case PrimType.Ring:
                    //GENERATE
                    var genKey = primitive.PrimData;
                    _generated[genKey] = mesh;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public void Add(Primitive key, Mesh value) => Set(key, value);

    public bool ContainsKey(Primitive key)=> TryGet(key, out _);

    public bool Remove(Primitive key)
    {
        throw new NotImplementedException();
    }

    public bool TryGetValue(Primitive key, out Mesh value) => TryGet(key, out value);

    public Mesh this[Primitive key]
    {
        get => TryGet(key, out var mesh) ? mesh : throw new KeyNotFoundException();
        set => Set(key, value);
    }

    public ICollection<Primitive> Keys =>  throw new NotImplementedException();

    public ICollection<Mesh> Values => throw new NotImplementedException();
}

[RequireComponent(typeof(SLObjectManager))]
public class SLMeshManager : SLBehaviour
{
    
    private static readonly IRendering MeshGen = new MeshmerizerR();
    private MeshCache _cache;
    private ThreadDictionary<UUID, Action<Mesh>> _callbacks; //THIS IS A PRETTY GARBAGE HACK! 


    private void Awake()
    {
        _callbacks = new ThreadDictionary<UUID, Action<Mesh>>();
        _cache = new MeshCache();
        MeshCreated += TryCallback;
    }

    private void TryCallback(object sender, PrimMeshCreatedArgs e)
    {
        if (!_callbacks.TryGetValue(e.Owner.ID, out var callback)) return;
        callback(e.GeneratedMesh);
        _callbacks.Remove(e.Owner.ID);
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
        if (_cache.TryGetValue(primitive, out var mesh))
            callback(mesh);
        else
        {
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
                    GenerateMesh(primitive);
                    break;
                case PrimType.Sculpt:
                    throw new NotSupportedException();
                    break;
                case PrimType.Mesh:
                    DownloadMesh(primitive);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    


    public event EventHandler<PrimMeshCreatedArgs> MeshCreated;

    protected virtual void OnUnityMeshUpdated(PrimMeshCreatedArgs e)
    {
        MeshCreated?.Invoke(this, e);
    }
    private void DownloadMesh(Primitive primitive)
    {
        if (primitive == null)
            throw new NullReferenceException("Self has not been initialized.");
        if (primitive.Type != PrimType.Mesh)
            throw new InvalidOperationException("Non-Mesh Primitives cannot download a mesh!");
        Manager.Client.Assets.RequestMesh(primitive.Sculpt.SculptTexture,MeshDownloaded(primitive));
    }

    private AssetManager.MeshDownloadCallback MeshDownloaded(Primitive primitive)
    {
        void InternalCallback(bool success, AssetMesh assetMesh)
        {
            
            void DataThreadCallback()
            {
                Debug.Log("Decoding Mesh From Downloaded Data");
                if (FacetedMesh.TryDecodeFromAsset(primitive, assetMesh, DetailLevel.Highest, out var slMesh))
                {
                    // Debug.Log("DEBUG MESH: Downloaded!");
                    var uMeshData = UMeshData.FromSL(slMesh);
                    Manager.Threading.Unity.Global.Enqueue(()=>CreateMesh(primitive,uMeshData));
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
    [Obsolete]
    private void CreateMesh(Primitive primitive, FacetedMesh slMesh)
    {
        var uMesh = slMesh.ToUnity();
        _cache[primitive] = uMesh;
        OnUnityMeshUpdated(new PrimMeshCreatedArgs(primitive, uMesh));
    }
    private void CreateMesh(Primitive primitive, UMeshData uMesh)
    {
        Debug.Log("Creating Mesh From UMeshData");
        var m = uMesh.ToUnity();
        m.name = primitive.Type == PrimType.Mesh ? primitive.Sculpt.SculptTexture.ToString() : "Generated";
        _cache[primitive] = m;
        OnUnityMeshUpdated(new PrimMeshCreatedArgs(primitive, m));
    }
}