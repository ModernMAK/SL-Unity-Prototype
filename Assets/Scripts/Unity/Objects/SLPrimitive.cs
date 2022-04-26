using System;
using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.Rendering;
using Texture = UnityEngine.Texture;
using Mesh = UnityEngine.Mesh;

public class SLPrimitive : SLBehaviour
{
    private static readonly IRendering MeshGen = new MeshmerizerR();
    public Primitive Self { get; private set; }
    public Mesh UnityMesh { get; private set; }
    public Texture[] UnityTextures { get; private set; }

    public event EventHandler Initialized;

    protected virtual void Awake()
    {
        Initialized += StartRequests;
    }

    private void StartRequests(object sender, EventArgs e)
    {
        RequestMesh();
        RequestTextures();
    }


    public void Initialize(Primitive self)
    {
        if (Self != null)
            throw new ArgumentException("Primitive Object has already been initialized!", nameof(self));
        Self = self;
        OnInitialized();
    }

    public void RequestMesh()
    {
        if(UnityMesh == null)
            SLManager.Instance.MeshManager.RequestMesh(Self,MeshGenerated);
    }

    private void RequestTextures()
    {
        var fTexts = Self.Textures.FaceTextures;
        UnityTextures = new Texture[fTexts.Length];
        for(var i = 0; i < UnityTextures.Length; i++)
        {
            var textureInfo = (fTexts[i] ?? Self.Textures.DefaultTexture).TextureID;
            Manager.TextureManager.RequestTexture(textureInfo,TextureFetched(i));
        }
    }

    private Action<Texture> TextureFetched(int i)
    {
        void Internal(Texture texture)
        {
            UnityTextures[i] = texture;
            OnUnityTexturesUpdated(new UnityTexturesUpdatedArgs(i, texture));
        }

        return Internal;
    }


    private void MeshGenerated(Mesh obj)
    {
        UnityMesh = obj;
        OnUnityMeshUpdated(new UnityMeshUpdatedArgs(obj));
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