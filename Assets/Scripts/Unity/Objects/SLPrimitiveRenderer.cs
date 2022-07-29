using System;
using UnityEngine;

// [RequireComponent(typeof(SLPrimitive))]
[RequireComponent(typeof(MeshFilter))]
public class SLPrimitiveRenderer : SLBehaviour
{
    [SerializeField]
    private SLPrimitive _primitive;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private static readonly int BaseMap = Shader.PropertyToID("_AlbedoTex");
    // private static Shader SimpleShader;
    private static readonly string ShaderName = "Shader Graphs/DebugSL";
    private static Shader _shader;

    public static Shader Shader
    {
        get
        {
            if (_shader != null) return _shader;
            
            _shader = Shader.Find(ShaderName);
            
            if (_shader != null) return _shader;
            
            throw new NullReferenceException();
        }
    }
    private static object _renderLock;
    private void Awake()
    {
        _renderLock = new object();
        if (_primitive == null)
            _primitive = GetComponent<SLPrimitive>();
        
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _primitive.UnityMeshUpdated += PrimitiveOnUnityMeshUpdated;
        _primitive.UnityTexturesUpdated += PrimitiveOnUnityTexturesUpdated;
        // _primitive.Initialized += PrimitiveOnInitialized;
        // if (SimpleShader == null)
        //     SimpleShader = ;
        // if (SimpleShader == null)
        //     throw new NullReferenceException();
        // else
        // {
        //     Debug.Log(SimpleShader);
        //     Debug.Break();
        // }
    }

    // private void PrimitiveOnInitialized(object sender, EventArgs e)
    // {
    //     // return;
    //     
    //     // var mat = _meshRenderer.material;
    //     lock (_renderLock)
    //     {
    //         // if (SimpleShader == null)
    //         //     throw new NullReferenceException();
    //         //THIS IS A COPY?! GD IT UNITY! UPDATE YOUR FING DOCSTRINGS!
    //         var mats = new Material[_primitive.Self.Textures.FaceTextures.Length];
    //         for (var i = 0; i < mats.Length; i++)
    //         {
    //             mats[i] = new Material(Shader);
    //         }
    //
    //         _meshRenderer.materials = mats;
    //     }
    // }

    private void PrimitiveOnUnityTexturesUpdated(object sender, UnityTexturesUpdatedArgs e)
    {
        lock (_renderLock)
        {
            if (e.TextureIndex == -1)
            {
                foreach (var t in _meshRenderer.materials)
                    if(t.GetTexture(BaseMap) == null)
                        t.SetTexture(BaseMap, e.NewTexture);
            }
            else
            {
                _meshRenderer.materials[e.TextureIndex].SetTexture(BaseMap, e.NewTexture);
            }
        }

    }

    private void PrimitiveOnUnityMeshUpdated(object sender, UnityMeshUpdatedArgs e)
    {
        lock (_renderLock)
        {
            _meshFilter.mesh = e.NewMesh;
            var mats= new Material[e.NewMesh.subMeshCount];
            for (var i = 0; i < mats.Length; i++)
            {
                var m = mats[i] = new Material(Shader);
                if (_primitive.UnityTextures != null && _primitive.UnityTextures.Length > i)
                    m.SetTexture(BaseMap, _primitive.UnityTextures[i] ?? _primitive.UnityDefaultTexture);
            }
            _meshRenderer.materials = mats; //In addition to returning a copy/ seems to copy when assigning
        }

        // Debug.Log("DEBUG MESH: Updated!");
    }
}
