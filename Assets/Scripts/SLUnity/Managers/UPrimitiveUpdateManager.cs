using System;
using OpenMetaverse;
using SLUnity.Objects;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace SLUnity.Managers
{
    public class UPrimitiveUpdateManager : SLBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Transform _container;
        private UPrimitiveRegistry Registry => Manager.PrimitiveRegistry;

        private ObjectManager LibreManager => Client.Objects;

        private void Awake()
        {
            if (_prefab == null)
                throw new NullReferenceException("Prefab wasn't assigned!");
            if (_container == null)
            {
                _container = new GameObject("Primitives")
                {
                    transform = { position = Vector3.zero}
                }.transform;
            }
        }

        private void OnEnable()
        {
            LibreManager.ObjectUpdate += LibreManagerOnObjectUpdate;
        }

        private void OnDisable()
        {
            LibreManager.ObjectUpdate -= LibreManagerOnObjectUpdate;
        }

        private void LibreManagerOnObjectUpdate(object sender, PrimEventArgs e)
        {
            var isNew = e.IsNew;
            var doesExists = Registry.PrimitivesByUUID.TryGetValue(e.Prim.ID, out var uPrimitive);
            // var hasParent = e.Prim.ParentID != 0;
            // var parentExists = !hasParent || Registry.PrimitivesByLocalID.TryGetValue(e.Prim.ParentID, out var uParent);
            // var currentParent = doesExists ? uPrimitive.Transform.Parent : null;

            if (isNew)
            {
                if (doesExists)
                {
                    
                    //Update
                    var callback = GetUpdatePrimCallback(e.Prim, uPrimitive);
                    Manager.Threading.Unity.Enqueue(callback);
                }
                else //!doesExist
                {
                    //Create
                    var callback = GetCreatePrimCallback(e.Prim);
                    Manager.Threading.Unity.Enqueue(callback);
                }
            }
            else
            {
                if (doesExists)
                {
                    //Update
                    var callback = GetUpdatePrimCallback(e.Prim, uPrimitive);
                    Manager.Threading.Unity.Enqueue(callback);
                }
                else //!doesExist
                {
                    //Error
                    //Try - Catch required because an exception will probably break all events
                    try
                    {
                        throw new Exception($"Prim `{e.Prim.ID}` is not new, but does not exist!");
                    }
                    catch (Exception ex) 
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        public Action GetCreatePrimCallback(Primitive primitive) => (() => CreatePrim(primitive));
        public UPrimitive CreatePrim(Primitive primitive)
        {
            var uPrimObj = Instantiate(_prefab,Vector3.down * 255, Quaternion.identity, _container);
            var uPrim = uPrimObj.GetComponent<UPrimitive>();
            Registry.Add(uPrim,primitive);
            UpdatePrim(primitive,uPrim);
            uPrim.Initialize(primitive);
            return uPrim;
        }

        private Action GetUpdatePrimCallback(Primitive slPrimitive,UPrimitive uPrimitive ) =>
            () => UpdatePrim(slPrimitive,uPrimitive);
        private void UpdatePrim(Primitive slPrimitive,UPrimitive uPrimitive)
        {
            var t = uPrimitive.Transform;
            if(Registry.PrimitivesByLocalID.TryGetValue(slPrimitive.ParentID,out var parent))
                t.SetParent(parent.Transform);
            t.LocalPosition = CommonConversion.CoordToUnity(slPrimitive.Position);
            t.LocalRotation = CommonConversion.RotToUnity(slPrimitive.Rotation);
            t.Scale = CommonConversion.CoordToUnity(slPrimitive.Scale);
        }
    }
}