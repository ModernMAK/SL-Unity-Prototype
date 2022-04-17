using System;
using System.Collections.Generic;
using OpenMetaverse;
using UnityEngine;
using Material = UnityEngine.Material;

public class SLObjectManager : SLBehaviour
{
    private Dictionary<uint, SLPrimitive> _lookup;
    public event EventHandler<ObjectCreatedArgs> ObjectCreated;
    [SerializeField]
    private GameObject _container;

    [SerializeField] private Material _defaultMaterial;

    private Queue<Primitive> _primCreateQueue;
    private Queue<Primitive> _primParentifyQueue;
    private object PrimCreateQueueLock;
    private object PrimParentifyQueueLock;
    [SerializeField] private int _maxUpdates = 10;
    private bool _createCamera;

    private void Awake()
    {
        PrimCreateQueueLock = new object();
        PrimParentifyQueueLock = new object();
        if (_defaultMaterial == null)
            throw new Exception("Please set a material!");
        _primCreateQueue = new Queue<Primitive>();
        _primParentifyQueue = new Queue<Primitive>();
        _lookup = new Dictionary<uint, SLPrimitive>();
        if (_container == null)
        {
            _container = new GameObject("Primitives")
            {
                isStatic = true,
                transform =
                {
                    parent = transform //Default pos/rot should be unit 0 (origin) and identity quaternion (0x,0y,0z,1w) [which is (0x,0y,0z) in conventional euler rotations] 
                }
            };
        }
        
    }

    private void OnEnable()
    {
        Client.Objects.ObjectUpdate += ObjectsOnObjectUpdate;
        Client.Network.LoginProgress += NetworkOnLoginProgress;
    }

    private void NetworkOnLoginProgress(object sender, LoginProgressEventArgs e)
    {
        if (e.Status == LoginStatus.Success)
        {
            _createCamera = true;
        }
    }

    private void OnDisable()
    {
        Client.Objects.ObjectUpdate -= ObjectsOnObjectUpdate;
        Client.Network.LoginProgress -= NetworkOnLoginProgress;
    }

    private void ObjectsOnObjectUpdate(object sender, PrimEventArgs e)
    {
        var prim = e.Prim;
        if (prim == null)
            throw new NullReferenceException("Primitive updated is null!");
        if (e.IsNew)
        {
            if (_lookup.ContainsKey(prim.LocalID))
                throw new InvalidOperationException("Primitive Object state is invalid! A primitive is new but has already been created!");
            lock (_primCreateQueue)
            {
                // We have to wait to build the unity obj until we are executing on the main (unity) thread.
                _primCreateQueue.Enqueue(prim);
            }
            // var slPrimitive = _lookup[prim] = CreatePrimitiveObject(prim);
            // OnObjectCreated(new ObjectCreatedArgs(slPrimitive));
        }
        // else if(!_lookup.ContainsKey(prim))
        //     throw new InvalidOperationException("Primitive Object state is invalid! A primitive is not new but has not been created!");
    }

    private void FixedUpdate()
    {
        RunPrimCreate(_maxUpdates);
        RunPrimParentify(_maxUpdates);
        if (_createCamera)
        {
            _createCamera = false;
            var go = new GameObject("Camera");
            go.AddComponent<Camera>();
            go.transform.position = Client.Self.SimPosition.ToUnity();
            go.transform.rotation = Client.Self.SimRotation.ToUnity();
        }
    }

    private void RunPrimCreate(int updates = 1)
    {
        lock (PrimCreateQueueLock)
        {
            if (_primCreateQueue.Count == 0)
                return;
            for (var i = 0; i < updates && _primCreateQueue.Count > 0; i++)
            {
                var prim = _primCreateQueue.Dequeue();
                var slPrimitive = CreatePrimitiveObject(prim);
                _lookup[prim.LocalID] = slPrimitive;
                OnObjectCreated(new ObjectCreatedArgs(slPrimitive));
            }
        }
    }

    private void RunPrimParentify(int updates = 1)
    {

        lock (PrimParentifyQueueLock)
        {
            if (_primParentifyQueue.Count == 0)
                return;
            var failed = 0;
            for (var i = 0; i < updates && _primCreateQueue.Count - failed > 0; i++)
            {
                var prim = _primParentifyQueue.Dequeue();
                var SLPrim = _lookup[prim.LocalID];
                if (_lookup.TryGetValue(prim.ParentID, out var parentSLPrim))
                {
                    SetParent(SLPrim.gameObject, parentSLPrim.gameObject, prim);
                }
                else
                {
                    _primParentifyQueue.Enqueue(prim);
                    failed++;
                }
            }
        }
    }

    public InternalDictionary<uint, Primitive> Primitives => Client.Network.CurrentSim.ObjectsPrimitives;

    private SLPrimitive CreatePrimitiveObject(Primitive prim)
    {
        if (prim == null)
            throw new NullReferenceException("Cannot create a null primitive!");
        
        var go = new GameObject($"Primitive '{prim.ID.ToString()}'"); //Not visually appealing, but easy to identify
        var parentTransform = _container.transform;
        if (prim.ParentID != 0)
        {
            lock (PrimParentifyQueueLock)
            {
                if (_lookup.TryGetValue(prim.ParentID, out var parentPrim))
                    parentTransform = parentPrim.transform;
                else
                    _primParentifyQueue.Enqueue(prim);
            }
        }

        SetParent(go, parentTransform.gameObject, prim);
        var slPrimitive = go.AddComponent<SLPrimitive>();
        //Allow it to render, add mesh filter, rendere and the SL primitve render
        go.AddComponent<MeshFilter>(); //Contains mesh reference for draw calls
        var renderer = go.AddComponent<MeshRenderer>(); //sends draw calls
        renderer.material = _defaultMaterial;
        go.AddComponent<SLPrimitiveRenderer>(); // Mediator between MeshRenderer and SLPrimitive
        slPrimitive.Initialize(prim);
        return slPrimitive;
    }

    private void SetParent(GameObject go, GameObject parent, Primitive prim)
    {
        go.transform.parent = parent.transform;
        go.transform.localPosition = prim.Position.ToUnity();
        go.transform.localRotation = prim.Rotation.ToUnity();
        go.transform.localScale = prim.Scale.ToUnity();
    }

    protected virtual void OnObjectCreated(ObjectCreatedArgs e)
    {
        ObjectCreated?.Invoke(this, e);
    }
}

public class ObjectCreatedArgs : EventArgs
{
    public ObjectCreatedArgs(SLPrimitive primitive)
    {
        Primitive = primitive;
    }

    public SLPrimitive Primitive { get; }
}
