using System;
using System.Collections.Generic;
using OpenMetaverse;
using UnityEngine;

public class SLPrimitiveManager : SLBehaviour
{
    //LocalID seems to change, so use UUID
    //Then use Libre's Primtive table to fetchh parent?
    
    [SerializeField]
    private GameObject _primtivePrefab;
    private ThreadDictionary<UUID, SLPrimitive> _lookup;
    private ThreadSet<UUID> _promises;
    public IReadOnlyDictionary<UUID, SLPrimitive> PrimitiveObjects => (IReadOnlyDictionary<UUID,SLPrimitive>)_lookup;
    public event EventHandler<ObjectCreatedArgs> ObjectCreated;
    [SerializeField]
    private GameObject _container;

    private bool _createCamera;

    private void Awake()
    {
        _promises = new ThreadSet<UUID>();
        _lookup = new ThreadDictionary<UUID, SLPrimitive>();
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
        Manager.Client.Objects.ObjectUpdate += ObjectsOnObjectUpdate;
        Manager.Client.Network.LoginProgress += NetworkOnLoginProgress;
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
        Manager.Client.Objects.ObjectUpdate -= ObjectsOnObjectUpdate;
        Manager.Client.Network.LoginProgress -= NetworkOnLoginProgress;
    }

    private void ObjectsOnObjectUpdate(object sender, PrimEventArgs e)
    {
        var prim = e.Prim;
        if (prim == null)
            throw new NullReferenceException("Primitive updated is null!");
        if (e.IsNew)
        {
            if (_lookup.ContainsKey(prim.ID) || _promises.Contains(prim.ID))
                throw new InvalidOperationException("Primitive Object state is invalid! A primitive is new but has already been created!");
            _promises.Add(prim.ID);
            Manager.Threading.Unity.Global.Enqueue(() => CreatePrim(prim));
        }
        //May have a race condition since lookup/promises are synced separately
        else if (!_lookup.ContainsKey(prim.ID) && !_promises.Contains(prim.ID))
        {
            throw new InvalidOperationException("Primitive Object state is invalid! A primitive is not new but has not been created!");
        }
    }

    private void FixedUpdate()
    {
        if (_createCamera)
        {
            _createCamera = false;
            var go = new GameObject("Camera");
            go.AddComponent<Camera>();
            go.transform.position = Client.Self.SimPosition.ToUnity();
            go.transform.rotation = Client.Self.SimRotation.ToUnity();
        }
    }


    void CreatePrim(Primitive prim)
    {
        //We need to lock _lookup across CreatePrim
        SLPrimitive slPrimitive;
        var unsynced = _lookup.Unsynchronized;
        _promises.Remove(prim.ID);
        lock (_lookup.SyncRoot)
        {
            unsynced[prim.ID] = slPrimitive = CreatePrimitiveObject(prim);
        }
        OnObjectCreated(new ObjectCreatedArgs(slPrimitive));
    }

    private Action ParentifyPrimAction(Primitive prim)
    {
        void Wrapper() => ParentifyPrim(prim);
        return Wrapper;
    }
    private void ParentifyPrim(Primitive prim)
    {
        //We need to lock _lookup across Paerntify (to make sure parent isn't added between TryGetValues
        //Aggressive lock, could be made smaller
        var unsynched = _lookup.Unsynchronized;
        lock(_lookup.SyncRoot)
        {
            if (unsynched.TryGetValue(prim.ID, out var SLPrim))
            {
                //TODO make sure this is locked?
                //  Internal Dict -> It's Libre's ThreadDict; should be locked
                if(Manager.Client.Network.CurrentSim.ObjectsPrimitives.TryGetValue(prim.ParentID, out var parent))
                {
                    if (unsynched.TryGetValue(parent.ID, out var parentSLPrim))
                    {
                        SetParent(SLPrim.gameObject, parentSLPrim.gameObject, prim);
                    }
                    else
                    {
                        Debug.LogWarning("Parent wasn't created?");
                        Manager.Threading.Unity.Global.Enqueue(ParentifyPrimAction(prim));
                    }
                }
                else
                {
                    Debug.LogWarning("Prim Parent doesn't exist yet?");
                    Manager.Threading.Unity.Global.Enqueue(ParentifyPrimAction(prim));
                }
            }
            else if(_promises.Contains(prim.ID))
            {
                Debug.LogWarning("Waiting for prim to be constructed...");
                Manager.Threading.Unity.Global.Enqueue(ParentifyPrimAction(prim));
            }
            else
            {
                Debug.LogWarning("Can't parentify prim that hasn't been created and is not promised to be created! Creating Prim.");
                _promises.Add(prim.ID);
                Manager.Threading.Unity.Global.Enqueue(()=>CreatePrim(prim));
            }
        }
    }

    public InternalDictionary<uint, Primitive> Primitives => Client.Network.CurrentSim.ObjectsPrimitives;

    private SLPrimitive CreatePrimitiveObject(Primitive prim)
    {
        if (prim == null)
            throw new NullReferenceException("Cannot create a null primitive!");
        var go = Instantiate(_primtivePrefab, prim.Position.ToUnity(), prim.Rotation.ToUnity(), _container.transform);
        go.name = $"Primitive `{prim.ID}`";
        go.transform.localScale = prim.Scale.ToUnity();
        //TODO move to SLPrimitive
        //Parenting should belong to primitive and be done in initialize (this would also allow us to hide our instantiated object until all
        //  Actions are done
        Manager.Threading.Unity.Global.Enqueue(ParentifyPrimAction(prim));
        // go.transform.localScale = prim.Scale.ToUnity();
        // if (prim.ParentID != 0)
        // {
        //     if (_lookup.TryGetValue(prim., out var parentPrim))
        //         SetParent(go, parentPrim.gameObject, prim);
        //     else
        // }

        var slPrimitive = go.GetComponent<SLPrimitive>();
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
