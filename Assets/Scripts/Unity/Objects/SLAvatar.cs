using System;
using Attributes;
using OpenMetaverse;
using UnityEngine;
using Avatar = OpenMetaverse.Avatar;

namespace Unity.Objects
{
    public class SLAvatar : SLBehaviour
    {
        // private static readonly IRendering MeshGen = new MeshmerizerR();
        //
        // [SerializeField] [ReadOnly] private int _requestedTextures; //
        //
        // [SerializeField][ReadOnly]
        // private Mesh _mesh;
        // [SerializeField] [ReadOnly] private Texture[] _textures;
        // [SerializeField] [ReadOnly] private Texture _defaultTexture;
        [SerializeField] [ReadOnly] private bool _awoken = false;
        [SerializeField] [ReadOnly] private PrimType _pType;
        [SerializeField] [ReadOnly] private string _sculptTex;
        [SerializeField] [ReadOnly] private bool _isThisUser;
        public bool LocalUserAvatar { get => _isThisUser; private set => _isThisUser = value; }
        public Avatar Self { get; private set; }
        // public Mesh UnityMesh
        // {
        //     get => _mesh;
        //     private set => _mesh = value;
        // }
        // public Texture[] UnityTextures {
        //     get => _textures;
        //     private set => _textures = value;
        //     
        // }
        // public Texture UnityDefaultTexture {
        //     get => _defaultTexture;
        //     private set => _defaultTexture = value;
        //     
        // }
        //
        public event EventHandler Initialized;
        //
        protected virtual void Awake()
        {
            // _requestedTextures = 0;
            // _textures = null;
            // _defaultTexture = null;
            _awoken = true;
            Initialized += DoDebug;
        }

        private void DoDebug(object sender, EventArgs e)
        {
            _sculptTex = Self.Sculpt.SculptTexture.Guid.ToString();
            _pType = Self.Type;
        }

        //
        // private void StartRequests(object sender, EventArgs e)
        // {
        //     RequestMesh();
        //     RequestTextures();
        // }
        //
        //
        public void Initialize(Avatar self)
        {
            if (!_awoken)
                throw new Exception("Initialize occured before gameobject initialization finished!");
            if (Self != null)
                throw new ArgumentException("Primitive Object has already been initialized!", nameof(self));
            Self = self;
            LocalUserAvatar = (self.Name == Manager.Client.Self.Name); //HACK
            OnInitialized();
        }
        //
        // public void RequestMesh()
        // {
        //     void MeshGenerated(Mesh obj)
        //     {
        //         obj.name = Self.Type switch
        //         {
        //             //FOR debug purposes
        //             PrimType.Mesh => $"Mesh `{Self.Sculpt.SculptTexture}`", // Doubles as mesh asset for Mesh
        //             PrimType.Sculpt => $"Sculpt `{Self.Sculpt.GetHashCode()}`",
        //             PrimType.Unknown => $"Unknown Prim type ~ How was this generated!?",
        //             _ => $"Generated `{Self.Type}` `{Self.PrimData.GetHashCode()}`"
        //         };
        //         UnityMesh = obj;
        //         OnUnityMeshUpdated(new UnityMeshUpdatedArgs(obj));
        //     }
        //
        //     if(UnityMesh == null)
        //         SLManager.Instance.MeshManager.RequestMesh(Self,MeshGenerated);
        // }
        //
        // private void RequestTextures()
        // {
        //     void TextureFetched(Texture texture, int index)
        //     {
        //         texture.name = $"Texture `{Self.Textures.FaceTextures[index].TextureID}`";
        //         UnityTextures[index] = texture;
        //         OnUnityTexturesUpdated(new UnityTexturesUpdatedArgs(index, texture));
        //     }
        //     void DefaultTextureFetched(Texture texture)
        //     {
        //         texture.name = $"Texture `{Self.Textures.DefaultTexture.TextureID}`";
        //         UnityDefaultTexture = texture;
        //         //TODO
        //         // OnUnityTexturesUpdated(new UnityTexturesUpdatedArgs(null, texture));
        //     }
        //     
        //     var fTexts = Self.Textures.FaceTextures;
        //     UnityTextures = new Texture[fTexts.Length];
        //     
        //     var defTex = Self.Textures.DefaultTexture;
        //     Manager.TextureManager.RequestTexture(defTex.TextureID,DefaultTextureFetched);
        //     for(var i = 0; i < UnityTextures.Length; i++)
        //     {
        //         var faceTex = fTexts[i];
        //         if (faceTex == null)
        //             continue;
        //         _requestedTextures++;
        //         var texIndex = i;// Required to use i in Callback; i will change but the copy 'texIndex' will not
        //         Manager.TextureManager.RequestTexture(faceTex.TextureID,(tex)=>TextureFetched(tex,texIndex));
        //     }
        // }
        //
        //
        //
        //
        // public event EventHandler<UnityMeshUpdatedArgs> UnityMeshUpdated;
        // public event EventHandler<UnityTexturesUpdatedArgs> UnityTexturesUpdated;
        //
        // protected virtual void OnUnityMeshUpdated(UnityMeshUpdatedArgs e)
        // {
        //     UnityMeshUpdated?.Invoke(this, e);
        // }
        // protected virtual void OnUnityTexturesUpdated(UnityTexturesUpdatedArgs e)
        // {
        //     UnityTexturesUpdated?.Invoke(this, e);
        // }
        //
        //
        protected virtual void OnInitialized()
        {
            Initialized?.Invoke(this, EventArgs.Empty);
        }
    }
}