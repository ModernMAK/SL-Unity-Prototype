using System;
using System.Collections.Generic;
using OpenMetaverse;
using SLUnity.Objects;
using SLUnity.Threading;
using UnityEngine;

namespace SLUnity.Managers
{
    public class UPrimitiveRegistry : SLBehaviour
    {
        private ThreadDictionary<UUID, UPrimitive> _uuidLookup;
        private ThreadDictionary<uint, UPrimitive> _localIdLookup;
        public void Add(UPrimitive uPrim, Primitive slPrim)
        {
            if (_uuidLookup.ContainsKey(slPrim.ID))
                throw new Exception("Key Already Exists!");
            _uuidLookup[slPrim.ID] = uPrim;
            _localIdLookup[slPrim.LocalID] = uPrim;
        }
        public bool Remove(Primitive slPrim)
        {
            return _uuidLookup.Remove(slPrim.ID) && _localIdLookup.Remove(slPrim.LocalID);
        }
        public IReadOnlyDictionary<UUID, UPrimitive> PrimitivesByUUID => _uuidLookup;
        public IReadOnlyDictionary<uint, UPrimitive> PrimitivesByLocalID => _localIdLookup;
        
        private void Awake()
        {
            _uuidLookup = new ThreadDictionary<UUID, UPrimitive>();
            _localIdLookup = new ThreadDictionary<uint, UPrimitive>();
        }
    }

    public class SLPrimitiveManagerS : SLBehaviour
    {
        //LocalID seems to change, so use UUID
        //Then use Libre's Primtive table to fetchh parent?
        private const int NullParentID = 0; // See https://wiki.secondlife.com/wiki/ObjectUpdate
        [SerializeField]
        private GameObject _primtivePrefab;
        private ThreadDictionary<UUID, UPrimitive> _lookup;
        private ThreadSet<UUID> _promises;
        public IReadOnlyDictionary<UUID, UPrimitive> PrimitiveObjects => (IReadOnlyDictionary<UUID,UPrimitive>)_lookup;
        public event EventHandler<ObjectCreatedArgs> ObjectCreated;
        [SerializeField]
        private GameObject _container;
        [SerializeField]
        private GameObject _unparentedContainer;
        [SerializeField]
        private GameObject _parentedContainer;

        private bool _createCamera;

        private void Awake()
        {
            _promises = new ThreadSet<UUID>();
            _lookup = new ThreadDictionary<UUID, UPrimitive>();
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
            if (_unparentedContainer == null)
            {
                _unparentedContainer = new GameObject("Unparented")
                {
                    isStatic = true,
                    transform =
                    {
                        parent = _container.transform,
                        position = UnityEngine.Vector3.one * -1000
                    }
                };
            }
            if (_parentedContainer == null)
            {
                _parentedContainer = new GameObject("Parented")
                {
                    isStatic = true,
                    transform =
                    {
                        parent = _container.transform,
                    }
                };
            }
        
        }

        private void OnEnable()
        {
            Manager.Client.Objects.ObjectUpdate += ObjectsOnObjectUpdate;
            Manager.Client.Network.LoginProgress += NetworkOnLoginProgress;
        }


        private void OnDisable()
        {
            Manager.Client.Objects.ObjectUpdate -= ObjectsOnObjectUpdate;
            Manager.Client.Network.LoginProgress -= NetworkOnLoginProgress;
        }

        private void NetworkOnLoginProgress(object sender, LoginProgressEventArgs e)
        {
            if (e.Status == LoginStatus.Success)
            {
                _createCamera = true;
            }
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
            
                // throw new InvalidOperationException("Primitive Object state is invalid! A primitive is not new but has not been created!");
            }
            else if(_lookup.TryGetValue(prim.ID, out var slPrim))
            {
                Manager.Threading.Unity.Global.Enqueue(() => UpdatePrim(slPrim, prim));
            }
            else
            {
                throw new Exception($"No Prim `{e.Prim.ID}`");
            }
        }

        private void UpdatePrim(UPrimitive uPrim, Primitive prim)
        {
            var t = uPrim.Transform;
            t.Scale = CommonConversion.CoordToUnity(prim.Scale); 
            t.LocalPosition = CommonConversion.CoordToUnity(prim.Position);
            t.LocalRotation = CommonConversion.RotToUnity(prim.Rotation);
        }

        private void FixedUpdate()
        {
            if (_createCamera)
            {
                _createCamera = false;
                var go = new GameObject("Camera");
                go.AddComponent<Camera>();
                go.transform.position = CommonConversion.CoordToUnity(Manager.Client.Self.SimPosition);
                go.transform.rotation = CommonConversion.RotToUnity(Manager.Client.Self.SimRotation);
            }
        }


        void CreatePrim(Primitive prim)
        {
            //We need to lock _lookup across CreatePrim
            UPrimitive uPrimitive;
            var unsynced = _lookup.Unsynchronized;
            _promises.Remove(prim.ID);
            lock (_lookup.SyncRoot)
            {
                unsynced[prim.ID] = uPrimitive = CreatePrimitiveObject(prim);
            }
            OnObjectCreated(new ObjectCreatedArgs(uPrimitive));
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
                    if (prim.ParentID == NullParentID)
                    {
                        SetParent(SLPrim.gameObject, _parentedContainer, prim);
                        SLPrim.gameObject.SetActive(true);
                    }
                    //TODO make sure this is locked?
                    //  Internal Dict -> It's Libre's ThreadDict; should be locked
                    else if(Manager.Client.Network.CurrentSim.ObjectsPrimitives.TryGetValue(prim.ParentID, out var parent))
                    {
                        //Parent primitive exists
                        if (unsynched.TryGetValue(parent.ID, out var parentSLPrim))
                        {
                            //SL Primitive exists
                            SetParent(SLPrim.gameObject, parentSLPrim.gameObject, prim);
                            SLPrim.gameObject.SetActive(true);
                        }
                        else if(_promises.Contains(parent.ID))
                        {
                            Debug.LogWarning("Waiting for parent prim to be constructed...");
                            Manager.Threading.Unity.Global.Enqueue(ParentifyPrimAction(prim));
                        }
                        else
                        {
                            //SL Primitive does not exist
                            Debug.LogWarning($"Parent `{parent.ID}` wasn't created?");
                            // _promises.Add(parent.ID);
                            // Manager.Threading.Unity.Global.Enqueue(()=>CreatePrim(parent));
                            // Manager.Threading.Unity.Global.Enqueue(ParentifyPrimAction(prim));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Parent `{prim.ParentID}` doesn't exist yet?");
                        // Manager.Client.Objects.RequestObject(Manager.Client.Network.CurrentSim, prim.ParentID);
                        // Manager.Threading.Unity.Global.Enqueue(ParentifyPrimAction(prim));
                    }
                }
                else if(_promises.Contains(prim.ID))
                {
                    Debug.LogWarning("Waiting for prim to be constructed...");
                    // Manager.Threading.Unity.Global.Enqueue(ParentifyPrimAction(prim));
                }
                else
                {
                    Debug.LogWarning("Can't parentify prim that hasn't been created and is not promised to be created! Creating Prim.");
                    // _promises.Add(prim.ID);
                    // Manager.Threading.Unity.Global.Enqueue(()=>CreatePrim(prim));
                }
            }
        }

        public InternalDictionary<uint, Primitive> Primitives => Manager.Client.Network.CurrentSim.ObjectsPrimitives;

        private UPrimitive CreatePrimitiveObject(Primitive prim)
        {
            if (prim == null)
                throw new NullReferenceException("Cannot create a null primitive!");
            var go = Instantiate(_primtivePrefab, _unparentedContainer.transform, true);
            // CommonConversion.CoordToUnity(prim.Position), 
            // CommonConversion.RotToUnity(prim.Rotation), 
            // _parentedContainer.transform);
            
            var slPrimitive = go.GetComponent<UPrimitive>();
            slPrimitive.Initialize(prim);
        
            //TODO move to SLPrimitive
            //Parenting should belong to primitive and be done in initialize (this would also allow us to hide our instantiated object until all
            //  Actions are done
            if (prim.ParentID != NullParentID)
            {
                SetParent(go,_unparentedContainer, prim);
                go.SetActive(false);
                Manager.Threading.Unity.Global.Enqueue(ParentifyPrimAction(prim));
            }
            else
            {
                SetParent(go,_parentedContainer, prim);
            }
            // go.transform.localScale = prim.Scale.ToUnity();
            // if (prim.ParentID != 0)
            // {
            //     if (_lookup.TryGetValue(prim., out var parentPrim))
            //         SetParent(go, parentPrim.gameObject, prim);
            //     else
            // }

            return slPrimitive;
        }

        private void SetParent(GameObject go, GameObject parent, Primitive prim)
        {
            var t = go.GetComponent<SLTransform>();
            var p = parent.GetComponent<SLTransform>();
            if (p == null)
                t.SetParent(parent.transform);
            else
                t.SetParent(p);
        
            // go.transform.parent = null;
            t.Scale = CommonConversion.CoordToUnity(prim.Scale); 
            // go.transform.SetParent(parent.transform,true);
            t.LocalPosition = CommonConversion.CoordToUnity(prim.Position);
            t.LocalRotation = CommonConversion.RotToUnity(prim.Rotation);
        }

        protected virtual void OnObjectCreated(ObjectCreatedArgs e)
        {
            ObjectCreated?.Invoke(this, e);
        }
    }
}