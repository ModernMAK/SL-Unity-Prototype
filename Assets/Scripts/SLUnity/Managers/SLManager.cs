using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SLUnity.Managers
{
    public class SLManager : MonoBehaviour
    {
        public static SLManager Instance { get; private set; }
        public SLClient Client { get; private set; }
        public SLTextureManager TextureManager { get; private set; }
        public SLPrimitiveManager PrimitiveManager { get; private set; }
        public SLAvatarManager AvatarManager { get; private set; }
        public SLMeshManager MeshManager { get; private set; }
        
        public SLThreadManager Threading { get; private set; }

        public SLControls Controls { get; private set; }

        private IEnumerable<Component> _managers
        {
            get
            {
                yield return Client;
                yield return TextureManager;
                yield return PrimitiveManager;
                yield return AvatarManager;
                yield return MeshManager;
                yield return Threading;
                yield return Controls;
            }
        }

        void AssertManagers()
        {
            if (_managers.Any(m => m == null))
            {
                throw new NullReferenceException()!;
            }
        }
        private void Awake()
        {
            Assertions.AssertSingleton(this,Instance,nameof(SLManager));
            Instance = this;
            Client = GetComponent<SLClient>();
            TextureManager = GetComponent<SLTextureManager>();
            PrimitiveManager = GetComponent<SLPrimitiveManager>();
            MeshManager = GetComponent<SLMeshManager>();
            Threading = GetComponent<SLThreadManager>();
            Controls = GetComponent<SLControls>();
            AssertManagers();
        }
    }
}