using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Managers
{
    public class SLManager : MonoBehaviour
    {
        public SLClient Client { get; private set; }
        public SLTextureManager TextureManager { get; private set; }
        public SLPrimitiveManager PrimitiveManager { get; private set; }
        public SLMeshManager MeshManager { get; private set; }

        public static SLManager Instance { get; private set; }

        public SLThreadManager Threading { get; private set; }

        private void Awake()
        {
            Assertions.AssertSingleton(this,Instance,nameof(SLManager));
            Instance = this;
            Client = GetComponent<SLClient>();
            TextureManager = GetComponent<SLTextureManager>();
            PrimitiveManager = GetComponent<SLPrimitiveManager>();
            MeshManager = GetComponent<SLMeshManager>();
            Threading = GetComponent<SLThreadManager>();
        }
    }
}