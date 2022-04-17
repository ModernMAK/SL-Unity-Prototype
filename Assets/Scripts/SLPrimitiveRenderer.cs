using UnityEngine;

[RequireComponent(typeof(SLPrimitive))]
[RequireComponent(typeof(MeshFilter))]
public class SLPrimitiveRenderer : SLBehaviour
{
    private SLPrimitive _primitive;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

    private void Awake()
    {
        _primitive = GetComponent<SLPrimitive>();
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _primitive.UnityMeshUpdated += PrimitiveOnUnityMeshUpdated;
        _primitive.UnityTexturesUpdated += PrimitiveOnUnityTexturesUpdated;
    }

    private void PrimitiveOnUnityTexturesUpdated(object sender, UnityTexturesUpdatedArgs e)
    {
        //Fix mats
        var materials = _meshRenderer.materials;
        var original = materials[e.TextureIndex];
        var newMaterial = materials[e.TextureIndex] = new Material(original);
        newMaterial.SetTexture(BaseMap, e.NewTexture);
        Debug.Log("DEBUG MAT: Updated!");
    }

    private void PrimitiveOnUnityMeshUpdated(object sender, UnityMeshUpdatedArgs e)
    {
        _meshFilter.mesh = e.NewMesh;
        //Fix mats
        var old = _meshRenderer.materials;
        _meshRenderer.materials = new Material[_meshFilter.mesh.subMeshCount];
        for (var i = 0; i < old.Length && i < _meshRenderer.materials.Length; i++)
        {
            _meshRenderer.materials[i] = old[i];
        }
        for (var i = old.Length; i < _meshRenderer.materials.Length; i++)
        {
            _meshRenderer.materials[i] = old[old.Length-1];
        }
        Debug.Log("DEBUG MESH: Updated!");
    }
}
