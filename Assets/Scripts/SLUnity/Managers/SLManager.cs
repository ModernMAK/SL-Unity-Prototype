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
        
        public DownloadManager Downloader { get; private set; }

        void AssertManagers()
        {
            void AssertManager(string name, object manager)
            {
                if (manager == null)
                    throw new NullReferenceException(name);
            }
            //Unfortunately; we lose name information if we iterate over an array helper containing these values; the price of sanity checks
            AssertManager(nameof(Client),Client);
            AssertManager(nameof(TextureManager),TextureManager);
            AssertManager(nameof(PrimitiveManager),PrimitiveManager);
            AssertManager(nameof(AvatarManager),AvatarManager);
            AssertManager(nameof(MeshManager),MeshManager);
            AssertManager(nameof(Threading),Threading);
            AssertManager(nameof(Controls),Controls);
        }
        private void Awake()
        {
            Assertions.AssertSingleton(this,Instance,nameof(SLManager));
            Instance = this;
            Downloader = new DownloadManager();
            Client = GetComponent<SLClient>();
            TextureManager = GetComponent<SLTextureManager>();
            PrimitiveManager = GetComponent<SLPrimitiveManager>();
            AvatarManager = GetComponent<SLAvatarManager>();
            MeshManager = GetComponent<SLMeshManager>();
            Threading = GetComponent<SLThreadManager>();
            Controls = GetComponent<SLControls>();
            
            AssertManagers();
        }
    }
}