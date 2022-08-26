using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using OpenMetaverse;
using OpenMetaverse.Rendering;
using SLUnity;
using SLUnity.Data;
using SLUnity.Managers;
using SLUnity.Objects;
using SLUnity.Rendering;
using SLUnity.Snapshot;
using UnityEngine;
using Mesh = UnityEngine.Mesh;
using Path = System.IO.Path;
using Vector2 = UnityEngine.Vector2;

public class RegionLoader : MonoBehaviour
{
    [SerializeField]
    private string _dir = "Assets/SLScene";
    [SerializeField]
    private string _file = "CurrentScene.uscene";

    [SerializeField] private GameObject _terrain;
    [SerializeField] private GameObject _primitive;
    [SerializeField] private GameObject _container;
    [SerializeField] private GameObject _invalid;

    private IHeightMapMeshGenerator _heightMapMeshGenerator;
    private IRendering _primRenderer;
    private AssetBundleDiskCache<UUID, Mesh> _meshCache;
    private AssetBundleDiskCache<UUID, Texture> _textureCache;
    private void Awake()
    {
        _meshCache = new AssetBundleDiskCache<UUID, Mesh>(
            "SLProtoAssets/Mesh/Highest",
            (uuid) => uuid.Guid.ToString(),
            (uuid) => uuid.Guid.ToString()
        );
        _textureCache = new AssetBundleDiskCache<UUID, Texture>(
            "SLProtoAssets/Texture",
            (uuid) => uuid.Guid.ToString(),
            (uuid) => uuid.Guid.ToString()
        );
        _primRenderer = new MeshmerizerR();
        
        _heightMapMeshGenerator = new TerrainMeshGenerator();
        _container = new GameObject("Primitives");
        _invalid = new GameObject("Bad Primitives");
        Load();
    }

    void Load()
    {
        var path = Path.Combine(_dir, _file);
        try
        {
            using var fstream = new FileStream(path, FileMode.Open);
            using var reader = new BinaryReader(fstream);
            var serilaizer = new SLRegionSerializer();
            var scene = serilaizer.Read(reader);
            var prims = scene.Primitives;
            var terrainData = scene.Terrain;

            var terrainMesh = _heightMapMeshGenerator.GenerateMesh(terrainData, Vector2.zero, Vector2.one * 256);
            var terrain = Instantiate(_terrain);
            terrain.GetComponent<MeshFilter>().mesh = terrainMesh;

            var primLookup = new Dictionary<UUID, GameObject>();
            var parentLookup = new Dictionary<uint, GameObject>();
            foreach (var prim in prims)
            {
                var obj = primLookup[prim.ID] = parentLookup[prim.LocalID] = Instantiate(_primitive);
                obj.transform.parent = _container.transform;
            }
            foreach (var prim in prims)
            {
                var parentIsPrim = false;
                var primObj = primLookup[prim.ID];
                GameObject parent;
                if (prim.ParentID == 0)
                    parent = _container;
                else if (parentLookup.TryGetValue(prim.ParentID, out parent))
                    parentIsPrim = true;
                else
                    parent = _invalid;
                var slPrim = primObj.GetComponent<SLPrimitive>();
                if (parentIsPrim)
                    slPrim.Transform.SetParent(parent.GetComponent<SLTransform>());
                else
                    slPrim.Transform.SetParent(parent.transform);
                var t = slPrim.Transform;
                t.Scale = CommonConversion.CoordToUnity(prim.Scale); 
                t.LocalPosition = CommonConversion.CoordToUnity(prim.Position);
                t.LocalRotation = CommonConversion.RotToUnity(prim.Rotation);
            }

            foreach (var prim in prims)
            {
                var primObj = primLookup[prim.ID];
                var slPrim = primObj.GetComponent<SLPrimitive>();
                slPrim.Self = prim;
                
                Mesh primMesh = null;
                switch (prim.Type)
                {
                    case PrimType.Sculpt:
                    case PrimType.Unknown:
                        primMesh = null;
                        break;
                    case PrimType.Mesh:
                    {
                        if (prim.Sculpt.SculptTexture == UUID.Zero || !_meshCache.Load(prim.Sculpt.SculptTexture, out primMesh))
                            primMesh = null;
                        break;
                    }
                    default:
                    {
                        var slmesh = _primRenderer.GenerateFacetedMesh(prim, DetailLevel.Highest);
                        var umesh = UMeshData.FromSL(slmesh);
                        primMesh = umesh.ToUnity();
                        break;
                    }
                }

                var slRenderer = slPrim.GetComponentInChildren<SLPrimitiveRenderer>();
                if (primMesh == null) continue;
                var defaultInfo = prim.Textures.DefaultTexture;
                _textureCache.Load(defaultInfo.TextureID, out var defaultTex);
                slRenderer.UpdateMesh(primMesh);
                for (var i = 0; i < primMesh.subMeshCount; i++)
                {
                    var faceInfo = prim.Textures.FaceTextures[i];
                    Texture faceTex;
                    if (faceInfo == null || faceInfo.TextureID == UUID.Zero)
                        faceTex = null;
                    else
                        _textureCache.Load(faceInfo.TextureID, out faceTex);
                    slRenderer.UpdateTexture(i,faceInfo,defaultInfo,faceTex,defaultTex);
                }
            }

            _invalid.SetActive(false);



        }
        catch (IOException exception)
        { 
            Debug.LogException(exception);   
        }
    }
}
