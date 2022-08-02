using System;
using Attributes;
using OpenMetaverse;
using OpenMetaverse.Rendering;
using SLUnity.Events;
using SLUnity.Managers;
using SLUnity.Threading;
using UnityEngine;
using Texture = UnityEngine.Texture;
using Mesh = UnityEngine.Mesh;

namespace SLUnity.Objects
{
    [RequireComponent(typeof(SLTransform))]
    public class SLPrimitive : SLBehaviour
    {
        private static readonly IRendering MeshGen = new MeshmerizerR();

        [SerializeField] [ReadOnly] private int _requestedTextures; //
    
        [SerializeField][ReadOnly]
        private Mesh _mesh;
        [SerializeField] [ReadOnly] private ThreadArray<Texture> _textures;
        [SerializeField] [ReadOnly] private ThreadVar<Texture> _defaultTexture;
        [SerializeField] [ReadOnly] private bool _awoken = false;
        public Primitive Self { get; private set; }
        public SLTransform Transform { get; private set; }
        public Mesh UnityMesh
        {
            get => _mesh;
            private set => _mesh = value;
        }
        public ThreadArray<Texture> UnityTextures {
            get => _textures;
            private set => _textures = value;
        
        }
        public ThreadVar<Texture> UnityDefaultTexture {
            get => _defaultTexture;
            private set => _defaultTexture = value;
        
        }

        public event EventHandler Initialized;

        protected virtual void Awake()
        {
            Transform = GetComponent<SLTransform>();
            _requestedTextures = 0;
            _textures = null;
            _defaultTexture = null;
            _awoken = true;
            Initialized += StartRequests;
        }

        private void StartRequests(object sender, EventArgs e)
        {
            _textures = new ThreadArray<Texture>(Self.Textures.FaceTextures.Length);
            _defaultTexture = new ThreadVar<Texture>();
            RequestMesh();
            RequestTextures();
        }


        public void Initialize(Primitive self)
        {   
            if (!_awoken)
                throw new Exception("Initialize occured before gameobject initialization finished!");
            if (Self != null)
                throw new ArgumentException("Primitive Object has already been initialized!", nameof(self));
            gameObject.name = $"Primitive `{self.ID}`";
            Self = self;
            OnInitialized();
        }

        public void RequestMesh()
        {
            void MeshGenerated(Mesh obj)
            {
                obj.name = Self.Type switch
                {
                    //FOR debug purposes
                    PrimType.Mesh => $"Mesh `{Self.Sculpt.SculptTexture}`", // Doubles as mesh asset for Mesh
                    PrimType.Sculpt => $"Sculpt `{Self.Sculpt.GetHashCode()}`",
                    PrimType.Unknown => $"Unknown Prim type ~ How was this generated!?",
                    _ => $"Generated `{Self.Type}` `{Self.PrimData.GetHashCode()}`"
                };
                UnityMesh = obj;
                OnUnityMeshUpdated(new UnityMeshUpdatedArgs(obj));
            }

            if(UnityMesh == null)
                SLManager.Instance.MeshManager.RequestMesh(Self,MeshGenerated);
        }

        private void RequestTextures()
        {
            void TextureFetched(Texture texture, int index, UUID id)
            {
                texture.name = $"Texture `{id}`";
                UnityTextures[index] = texture;
                OnUnityTexturesUpdated(new UnityTexturesUpdatedArgs(index, texture));
            }
            void DefaultTextureFetched(Texture texture, UUID id)
            {
                texture.name = $"Texture `{id}`";
                UnityDefaultTexture.Synchronized = texture;
                //TODO
                OnUnityTexturesUpdated(new UnityTexturesUpdatedArgs(-1, texture));
            }
        
            var fTexts = Self.Textures.FaceTextures;
        
            var defTex = Self.Textures.DefaultTexture;
            Manager.TextureManager.RequestTexture(defTex.TextureID,(tex) => DefaultTextureFetched(tex,defTex.TextureID));
            for(var i = 0; i < UnityTextures.Count; i++)
            {
                var faceTex = fTexts[i];
                if (faceTex == null)
                    continue;
                _requestedTextures++;
                var texIndex = i;// Required to use i in Callback; i will change but the copy 'texIndex' will not
                Manager.TextureManager.RequestTexture(faceTex.TextureID,(tex)=>TextureFetched(tex,texIndex,faceTex.TextureID));
            }
        }
    



        public event EventHandler<UnityMeshUpdatedArgs> UnityMeshUpdated;
        public event EventHandler<UnityTexturesUpdatedArgs> UnityTexturesUpdated;

        protected virtual void OnUnityMeshUpdated(UnityMeshUpdatedArgs e)
        {
            UnityMeshUpdated?.Invoke(this, e);
        }
        protected virtual void OnUnityTexturesUpdated(UnityTexturesUpdatedArgs e)
        {
            UnityTexturesUpdated?.Invoke(this, e);
        }


        protected virtual void OnInitialized()
        {
            Initialized?.Invoke(this, EventArgs.Empty);
        }
    }
}