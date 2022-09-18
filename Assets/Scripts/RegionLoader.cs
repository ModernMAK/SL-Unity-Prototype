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
        if(_container == null)
            _container = new GameObject("Primitives");
        if(_invalid == null)
            _invalid = new GameObject("Bad Primitives");
    }

    private void OnEnable()
    {
        Load();
    }


    void Load()
    {
        var path = Path.Combine(_dir, _file);
        try
        {
            var manager = SLManager.Instance;
            using var fstream = new FileStream(path, FileMode.Open);
            using var reader = new BinaryReader(fstream);
            var serilaizer = new SLRegionSerializer();
            var scene = serilaizer.Read(reader);
            var prims = scene.Primitives;
            var terrainData = scene.Terrain;
            Debug.Log("Terrain Heightmap Loaded");
            SLManager.Instance.Terrain.SetTerrainHeightmap(terrainData);
            // var terrainMesh = _heightMapMeshGenerator.GenerateMesh(terrainData, Vector2.zero, Vector2.one * 256);
            // var terrain = Instantiate(_terrain);
            // terrain.GetComponent<MeshFilter>().mesh = terrainMesh;
            Debug.Log("Mesh / Texture Requests Begun");

            //Bypass creating a prim to fake the Request to the MeshCache
            Dictionary<UUID, Primitive> mesh2Prim = new Dictionary<UUID, Primitive>();

            Dictionary<UUID, int> textureMode = new Dictionary<UUID, int>();
            Dictionary<UUID, int> meshMode = new Dictionary<UUID, int>();
            List<UUID> meshes = new List<UUID>();
            List<UUID> textures = new List<UUID>();
            // Load in all Meshes / Textures
            //  Prioritize reused assets
            foreach (var prim in prims)
            {
                
                if(prim.Type == PrimType.Mesh && prim.ID != UUID.Zero)
                {
                    var meshID = prim.Sculpt.SculptTexture;
                    if (meshMode.ContainsKey(meshID))
                        meshMode[meshID]++;
                    else
                    {
                        meshMode[meshID] = 1;
                        meshes.Add(meshID);
                        mesh2Prim[meshID] = prim;
                    }
                }
                var defTex = prim.Textures.DefaultTexture;
                var faceTexs = prim.Textures.FaceTextures;
                if(defTex != null && defTex.TextureID != UUID.Zero)
                {
                    if (textureMode.ContainsKey(defTex.TextureID))
                        textureMode[defTex.TextureID]++;
                    else
                    {
                        textureMode[defTex.TextureID] = 1;
                        textures.Add(defTex.TextureID);
                    }
                }
                foreach(var faceTex in faceTexs)
                    if(faceTex != null && faceTex.TextureID != UUID.Zero)
                    {
                        if (textureMode.ContainsKey(faceTex.TextureID))
                            textureMode[faceTex.TextureID]++;
                        else
                        {
                            textureMode[faceTex.TextureID] = 1;
                            textures.Add(faceTex.TextureID);
                        }
                    }
            }
            //High to Low
            meshes.Sort(((l, r) => -meshMode[l].CompareTo(meshMode[r])));
            textures.Sort(((l, r) => -textureMode[l].CompareTo(textureMode[r])));

            
            foreach(var meshID in meshes)
            {
                if(meshMode[meshID] == 1)
                    break; // Dont preload assets only used once
                manager.MeshManager.RequestMesh(mesh2Prim[meshID],(mesh => { }));
            }
            foreach(var texID in textures)
            {
                if(textureMode[texID] == 1)
                    break; // Dont preload assets only used once
                manager.TextureManager.RequestTexture(texID,(tex => { }));
            }
            
            // var meshIndex = 0;
            // var texIndex = 0;
            // while (meshIndex < meshes.Count && texIndex < textures.Count)
            // {
            //     var meshID = meshes[meshIndex];
            //     var texID = textures[texIndex];
            //     var meshFreq = meshMode[meshID];
            //     var texFreq = textureMode[texID];
            //
            //     if (meshFreq >= texFreq)
            //     {
            //         manager.MeshManager.RequestMesh(mesh2Prim[meshID],(mesh => { }));
            //         meshIndex++;
            //     }
            //     else
            //     {
            //         manager.TextureManager.RequestTexture(texID,(tex => { }));
            //         texIndex++;
            //     }
            //
            // }
            // for(;meshIndex < meshes.Count; meshIndex++)
            // {
            //     var meshID = meshes[meshIndex];
            //     manager.MeshManager.RequestMesh(mesh2Prim[meshID],(mesh => { }));
            // }
            // for(;texIndex < textures.Count; texIndex++)
            // {
            //     var texID = textures[texIndex];
            //     manager.TextureManager.RequestTexture(texID,(tex => { }));
            // }
            
            // This is the buggiest part of the code; because our 'Requests' want to create Prims, but we also want to create prims
            //      It would be best to Create all prims first from the graph; then register, then allow SL to update; but then we have to cache the evnts and replay them
            // Debug.Log("Prim Creation Requests Begun");
            // foreach (var prim in prims)
            //     manager.Threading.Unity.Enqueue(SLManager.Instance.PrimitiveManager.GetCreatePrimCallback(prim));

        }
        catch (IOException exception)
        { 
            Debug.LogException(exception);   
        }
    }
}
