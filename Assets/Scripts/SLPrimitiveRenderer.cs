using System;
using UnityEngine;

[RequireComponent(typeof(SLPrimitive))]
[RequireComponent(typeof(MeshFilter))]
public class SLPrimitiveRenderer : SLBehaviour
{
    private SLPrimitive _primitive;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private static readonly int BaseMap = Shader.PropertyToID("_AlbedoTex");
    private static Shader SimpleShader;
    private static readonly string ShaderName = "DebugSL";
    private void Awake()
    {
        _primitive = GetComponent<SLPrimitive>();
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _primitive.UnityMeshUpdated += PrimitiveOnUnityMeshUpdated;
        _primitive.UnityTexturesUpdated += PrimitiveOnUnityTexturesUpdated;
        _primitive.Initialized += PrimitiveOnInitialized;
        if (SimpleShader == null)
            SimpleShader = Manager.ObjectManager.DefaultShader;
        if (SimpleShader == null)
            throw new NullReferenceException();
    }

    private void PrimitiveOnInitialized(object sender, EventArgs e)
    {
        // var mat = _meshRenderer.material;
        _meshRenderer.materials = new Material[_primitive.Self.Textures.FaceTextures.Length];
        for (var i = 0; i < _meshRenderer.materials.Length; i++)
            _meshRenderer.materials[i] = new Material(SimpleShader);
    }

    private void PrimitiveOnUnityTexturesUpdated(object sender, UnityTexturesUpdatedArgs e)
    {
        //Fix mats
        // var materials = _meshRenderer.materials;
        // var original = materials[e.TextureIndex];
        // var newMaterial = materials[e.TextureIndex] = new Material(original);
        _meshRenderer.materials[e.TextureIndex].SetTexture(BaseMap, e.NewTexture);
        Debug.Log("DEBUG MAT: Updated!");
    }

    private void PrimitiveOnUnityMeshUpdated(object sender, UnityMeshUpdatedArgs e)
    {
        _meshFilter.mesh = e.NewMesh;
        Debug.Log("DEBUG MESH: Updated!");
    }
}
