using System;
using System.Collections.Generic;
using Attributes;
using OpenMetaverse;
using SLUnity.Events;
using SLUnity.Objects;
using SLUnity.Threading;
using UnityEngine;
using Avatar = OpenMetaverse.Avatar;

namespace SLUnity.Managers
{
    public class SLAvatarManager : SLBehaviour
    {
        //LocalID seems to change, so use UUID
        //Then use Libre's Avatartive table to fetchh parent?
    
        [SerializeField]
        private GameObject _avatarPrefab;
        private ThreadDictionary<UUID, SLAvatar> _lookup;
        private ThreadSet<UUID> _promises;
        public IReadOnlyDictionary<UUID, SLAvatar> AvatarObjects => (IReadOnlyDictionary<UUID,SLAvatar>)_lookup;
        public event EventHandler<AvatarCreatedArgs> ObjectCreated;
        [SerializeField]
        private GameObject _container;

        [SerializeField] [ReadOnly] private SLAvatar _localAvatar;


        private void Awake()
        {
            _promises = new ThreadSet<UUID>();
            _lookup = new ThreadDictionary<UUID, SLAvatar>();
            if (_container == null)
            {
                _container = new GameObject("Avatars")
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
            Manager.Client.Objects.AvatarUpdate +=ObjectsOnAvatarUpdate ;
        }



        private void OnDisable()
        {
            Manager.Client.Objects.AvatarUpdate -= ObjectsOnAvatarUpdate;
        }

        private void ObjectsOnAvatarUpdate(object sender, AvatarUpdateEventArgs e)
        {
            var avatar = e.Avatar;
            if (avatar == null)
                throw new NullReferenceException("Avatar updated is null!");
            if (e.IsNew)
            {
                if (_lookup.ContainsKey(avatar.ID) || _promises.Contains(avatar.ID))
                    throw new InvalidOperationException("Avatar Object state is invalid! A Avatar is new but has already been created!");
                _promises.Add(avatar.ID);
                Manager.Threading.Unity.Global.Enqueue(() => CreateAvatar(avatar));
            }
            //May have a race condition since lookup/promises are synced separately
            else if (!_lookup.ContainsKey(avatar.ID) && !_promises.Contains(avatar.ID))
            {
                throw new InvalidOperationException("Avatar Object state is invalid! A Avatar is not new but has not been created!");
            }
        }


        void CreateAvatar(Avatar avatar)
        {
            //We need to lock _lookup across CreateAvatar
            SLAvatar slAvatar;
            var unsynced = _lookup.Unsynchronized;
            _promises.Remove(avatar.ID);
            lock (_lookup.SyncRoot)
            {
                unsynced[avatar.ID] = slAvatar = CreateAvatarObject(avatar);
                if (slAvatar.LocalUserAvatar)
                {
                    _localAvatar = slAvatar;
                }
            }
            OnObjectCreated(new AvatarCreatedArgs(slAvatar));
        }


        public InternalDictionary<uint, Avatar> Avatars => Manager.Client.Network.CurrentSim.ObjectsAvatars;

        private SLAvatar CreateAvatarObject(Avatar avatar)
        {
            if (avatar == null)
                throw new NullReferenceException("Cannot create a null Avatar!");
            var go = Instantiate(
                _avatarPrefab, 
                CommonConversion.CoordToUnity(avatar.Position), 
                CommonConversion.RotToUnity(avatar.Rotation), 
                _container.transform
            );
            go.name = $"Avatar `{avatar.Name}`";
            go.transform.localScale = CommonConversion.CoordToUnity(avatar.Scale);
            //TODO move to SLAvatar
            //Parenting should belong to Avatar and be done in initialize (this would also allow us to hide our instantiated object until all
            //  Actions are done
            // Manager.Threading.Unity.Global.Enqueue(ParentifyAvatarAction(avatar));
            // go.transform.localScale = avatar.Scale.ToUnity();
            // if (avatar.ParentID != 0)
            // {
            //     if (_lookup.TryGetValue(avatar., out var parentAvatar))
            //         SetParent(go, parentAvatar.gameObject, avatar);
            //     else
            // }

            var slAvatar = go.GetComponent<SLAvatar>();
            slAvatar.Initialize(avatar);
            return slAvatar;
        }

        // private void SetParent(GameObject go, GameObject parent, Avatar avatar)
        // {
        //     go.transform.parent = parent.transform;
        //     go.transform.localPosition = avatar.Position.ToUnity();
        //     go.transform.localRotation = avatar.Rotation.ToUnity();
        //     go.transform.localScale = avatar.Scale.ToUnity();
        // }

        protected virtual void OnObjectCreated(AvatarCreatedArgs e)
        {
            ObjectCreated?.Invoke(this, e);
        }
    }
}