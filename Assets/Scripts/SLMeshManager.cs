using System;
using System.Collections;
using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;
using UnityEngine;
using Mesh = UnityEngine.Mesh;
using Object = System.Object;

public class MeshCache : IDictionary<Primitive,Mesh>
{
    private Dictionary<Primitive.ConstructionData, Mesh> _generated;
    private Dictionary<UUID, Mesh> _assets;

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

    public int Count => _assets.Count + _generated.Count;

    public bool IsReadOnly => throw new NotImplementedException();

    private Mesh Get(Primitive primitive)
    {
        return TryGetValue(primitive, out var mesh) ? mesh : null;
    }
    private bool TryGet(Primitive primitive, out Mesh mesh)
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
    private void Set(Primitive primitive, Mesh mesh)
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
    
    public int MaxDownloadsPerTick = 8;
    public int MaxRequestsPerTick = 8;
    private static readonly IRendering MeshGen = new MeshmerizerR();
    private MeshCache _cache;
    private Queue<Tuple<Primitive,FacetedMesh>> _downloadedQueue;
    private Queue<Primitive> _requestQueue;
    private object _downloadedQueueLock;
    private object _requestQueueLock;
    private Dictionary<UUID, Action<Mesh>> _callbacks; //THIS IS A PRETTY GARBAGE HACK! 


    private void Awake()
    {
        _callbacks = new Dictionary<UUID, Action<Mesh>>();
        _cache = new MeshCache();
        _downloadedQueueLock = new object();
        _requestQueueLock = new object();
        lock (_downloadedQueueLock)
        {
            _downloadedQueue = new Queue<Tuple<Primitive,FacetedMesh>>();
            
        }

        lock (_requestQueueLock)
        {
            _requestQueue = new Queue<Primitive>();
        }
        MeshCreated += TryCallback;
    }

    private void TryCallback(object sender, PrimMeshCreatedArgs e)
    {
        if (!_callbacks.TryGetValue(e.Owner.ID, out var callback)) return;
        callback(e.GeneratedMesh);
        _callbacks.Remove(e.Owner.ID);
    }

    private void FixedUpdate()
    {
        Primitive prim;
        lock (_downloadedQueueLock)
        {
            var downloads = 0;
            while (_downloadedQueue.Count > 0 && downloads < MaxDownloadsPerTick)
            {
                downloads++;
                var primMeshTuple = _downloadedQueue.Dequeue();
                prim = primMeshTuple.Item1;
                var mesh = primMeshTuple.Item2;
                CreateMesh(prim,mesh);
            }
        }

        lock (_requestQueueLock)
        {
            var requests = 0;
            while (_requestQueue.Count > 0 && requests < MaxRequestsPerTick)
            {
                requests++;
                prim = _requestQueue.Dequeue();
                switch (prim.Type)
                {
                    case PrimType.Mesh:
                        DownloadMesh(prim);
                        break;
                    case PrimType.Sculpt:
                    case PrimType.Unknown:
                        return;
                    case PrimType.Box:
                    case PrimType.Cylinder:
                    case PrimType.Prism:
                    case PrimType.Sphere:
                    case PrimType.Torus:
                    case PrimType.Tube:
                    case PrimType.Ring:
                        GenerateMesh(prim);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(prim.Type));
                }
            }
        }
    }

    public void RequestMesh(Primitive primitive, Action<Mesh> callback)
    {
        if (_cache.TryGetValue(primitive, out var mesh))
            callback(mesh);
        else
        {
            lock (_requestQueueLock)
            {
                _requestQueue.Enqueue(primitive);
                _callbacks[primitive.ID] = callback;
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
            if (success)
            {
                if (FacetedMesh.TryDecodeFromAsset(primitive, assetMesh, DetailLevel.Highest, out var slMesh))
                {
                    Debug.Log("DEBUG MESH: Downloaded!");
                    lock (_downloadedQueueLock)
                    {
                        _downloadedQueue.Enqueue(new Tuple<Primitive, FacetedMesh>(primitive, slMesh)); // Have to build our mesh on the main thread
                    }
                }
                else
                {
                    //TODO debug
                    Debug.Log("DEBUG MESH:Linden Mesh decoding failed!");
                }
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
        var slMesh = MeshGen.GenerateFacetedMesh(primitive, DetailLevel.Highest);
        CreateMesh(primitive, slMesh);
    }
    private void CreateMesh(Primitive primitive, FacetedMesh slMesh)
    {
        var uMesh = slMesh.ToUnity();
        _cache[primitive] = uMesh;
        OnUnityMeshUpdated(new PrimMeshCreatedArgs(primitive, uMesh));
    }
}
