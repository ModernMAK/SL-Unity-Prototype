using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenMetaverse;
using OpenMetaverse.ImportExport.Collada14;
using OpenMetaverse.Rendering;
using SLUnity;
using SLUnity.Data;
using SLUnity.Managers;
using SLUnity.Objects;
using SLUnity.Rendering;
using SLUnity.Serialization;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.PlayerLoop;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


class PrimSnapshot
{
    public UUID ID;
    
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public PrimType Type;
    public UUID MeshID;
    public Primitive.ConstructionData MeshData;

    public Primitive.TextureEntryFace DefaultTexture;
    public Primitive.TextureEntryFace[] Textures;
}

class Snapshot
{
    public PrimSnapshot[] Primitives;
    public float[,] Terrain;
}

class MeshDataSerializer : ISerializer<Primitive.ConstructionData>
{
    public void Write(BinaryWriter writer, Primitive.ConstructionData meshData)
    {
        writer.Write(meshData.profileCurve);
        writer.Write(meshData.State);
        writer.Write(meshData.PathBegin);
        writer.Write(meshData.PathEnd);
        writer.Write(meshData.PathRevolutions);
        writer.Write(meshData.PathSkew);
        writer.Write(meshData.PathTwist);
        writer.Write(meshData.ProfileBegin);
        writer.Write(meshData.ProfileEnd);
        writer.Write(meshData.ProfileHollow);
        writer.Write(meshData.PathRadiusOffset);
        writer.Write(meshData.PathScaleX);
        writer.Write(meshData.PathScaleY);
        writer.Write(meshData.PathShearX);
        writer.Write(meshData.PathShearY);
        writer.Write(meshData.PathTaperX);
        writer.Write(meshData.PathTaperY);
        writer.Write(meshData.PathTwistBegin);
        writer.Write((byte)meshData.Material);
        writer.Write((byte)meshData.AttachmentPoint);
        writer.Write((byte)meshData.PathCurve);
        writer.Write((byte)meshData.PCode);
        writer.Write((byte)meshData.ProfileCurve);
        writer.Write((byte)meshData.ProfileHole);
    }
    public Primitive.ConstructionData Read(BinaryReader reader)
    {
        return new Primitive.ConstructionData()
        {
            profileCurve = reader.ReadByte(),
            State = reader.ReadByte(),
            PathBegin = reader.ReadSingle(),
            PathEnd = reader.ReadSingle(),
            PathRevolutions = reader.ReadSingle(),
            PathSkew = reader.ReadSingle(),
            PathTwist = reader.ReadSingle(),
            ProfileBegin = reader.ReadSingle(),
            ProfileEnd = reader.ReadSingle(),
            ProfileHollow = reader.ReadSingle(),
            PathRadiusOffset = reader.ReadSingle(),
            PathScaleX = reader.ReadSingle(),
            PathScaleY = reader.ReadSingle(),
            PathShearX = reader.ReadSingle(),
            PathShearY = reader.ReadSingle(),
            PathTaperX = reader.ReadSingle(),
            PathTaperY = reader.ReadSingle(),
            PathTwistBegin = reader.ReadSingle(),
            Material = (OpenMetaverse.Material)reader.ReadByte(),
            AttachmentPoint = (AttachmentPoint)reader.ReadByte(),
            PathCurve = (PathCurve)reader.ReadByte(),
            PCode = (PCode)reader.ReadByte(),
            ProfileCurve = (ProfileCurve)reader.ReadByte(),
            ProfileHole = (HoleType)reader.ReadByte(),
        };
    }
}

class TextureSerializer : ISerializer<Primitive.TextureEntryFace>
{

    public void Write(BinaryWriter writer, Primitive.TextureEntryFace texData)
    {
        var isNull = texData == null;
        writer.Write(isNull);
        if(isNull)
            return;
        writer.Write(texData.Fullbright);
        writer.Write(texData.Glow);
        writer.Write(texData.Rotation);
        writer.Write(texData.MediaFlags);
        writer.Write(texData.OffsetU);
        writer.Write(texData.OffsetV);
        writer.Write(texData.RepeatU);
        writer.Write(texData.RepeatV);
        writer.Write((byte)texData.Bump);
        writer.Write((byte)texData.Shiny);
        writer.Write(texData.MaterialID.Guid);
        writer.Write((byte)texData.TexMapType);
        writer.Write(texData.TextureID.Guid);
        writer.Write(texData.RGBA.CastUnity());

    }
    public Primitive.TextureEntryFace Read(BinaryReader reader)
    {
        var isNull = reader.ReadBoolean();
        if (isNull)
            return null;
        return new Primitive.TextureEntryFace(null)
        {
            Fullbright = reader.ReadBoolean(),
            Glow = reader.ReadSingle(),
            Rotation = reader.ReadSingle(),
            MediaFlags = reader.ReadBoolean(),
            OffsetU = reader.ReadSingle(),
            OffsetV = reader.ReadSingle(),
            RepeatU = reader.ReadSingle(),
            RepeatV = reader.ReadSingle(),
            Bump = (Bumpiness)reader.ReadByte(),
            Shiny = (Shininess)reader.ReadByte(),
            MaterialID = new UUID(reader.ReadGuid()),
            TexMapType = (MappingType)reader.ReadByte(),
            TextureID = new UUID(reader.ReadGuid()),
            RGBA = reader.ReadColor32().CastSL(),
        };
    }

}
class PrimSnapshotSerializer : ISerializer<PrimSnapshot>
{
    private MeshDataSerializer _meshSerializer;
    private TextureSerializer _texSerializer;
    public PrimSnapshotSerializer()
    {
        _meshSerializer = new MeshDataSerializer();
        _texSerializer = new TextureSerializer();
    }
    
    public void Write(BinaryWriter writer, PrimSnapshot obj)
    {
        writer.Write(obj.ID.Guid);
        writer.Write(obj.Position);
        writer.Write(obj.Rotation);
        writer.Write(obj.Scale);
        writer.Write((byte)obj.Type);
        writer.Write(obj.MeshID.Guid);
        _meshSerializer.Write(writer, obj.MeshData);
        _texSerializer.Write(writer,obj.DefaultTexture);
        writer.WriteArray(obj.Textures,_texSerializer.Write);
    }
    public PrimSnapshot Read(BinaryReader reader)
    {
        //CAnt inline because it makes debugging impossible
        var ID = new UUID(reader.ReadGuid());
        var Position = reader.ReadVector3();
        var Rotation = reader.ReadQuaternion();
        var Scale = reader.ReadVector3();
        var _Type = (PrimType)reader.ReadByte();
        var MeshID = new UUID(reader.ReadGuid());
        var MeshData = _meshSerializer.Read(reader);
        var DefaultTexture = _texSerializer.Read(reader);
        var Textures = reader.ReadArray(_texSerializer.Read);
        
        return new PrimSnapshot()
        {
            ID = ID,
            Position = Position,
            Rotation = Rotation,
            Scale = Scale,
            Type = _Type,
            MeshID = MeshID,
            MeshData = MeshData,
            DefaultTexture = DefaultTexture,
            Textures = Textures
        };
    }

}
class SnapshotSerializer : ISerializer<Snapshot>
{
    private PrimSnapshotSerializer _serializer;

    public SnapshotSerializer()
    {
        _serializer = new PrimSnapshotSerializer();
    }

    public void Write(BinaryWriter writer, Snapshot value)
    {
        writer.WriteArray(value.Primitives,_serializer.Write);
        writer.WriteArray2D(value.Terrain,writer.Write);
    }

    public Snapshot Read(BinaryReader reader)
    {
        return new Snapshot()
        {
            Primitives = reader.ReadArray(_serializer.Read),
            Terrain = reader.ReadArray2D(reader.ReadSingle)
        };
    }


}
[ExecuteInEditMode]
public class SnapshotForAssetBundle : MonoBehaviour
{

    public string Path;
    public bool Save;
    public bool Load;

    void Update()
    {
        if (Save)
        {
            Save = false;
            SaveSnap();
        }

        if (Load)
        {
            Load = false;
            LoadSnap();
        }
    }
    
    void SaveSnap()
    {
        Debug.Break();
        List<PrimSnapshot> list = new List<PrimSnapshot>();
        foreach (var slPrimitive in FindObjectsOfType<UPrimitive>())
        {
            Primitive p = slPrimitive.Self;
            SLTransform t = slPrimitive.Transform;
            if (p == null)
                throw new NullReferenceException();
            if (t == null)
                throw new NullReferenceException();
            var ID = p.ID;
            var _Type = p.Type;
            var Position = t.WorldPosition;
            var Rotation = t.WorldRotation;
            var Scale = t.Scale;
            var MeshData = p.PrimData;
            var MeshID = p.Sculpt?.SculptTexture;
            var DefaultTexture = p.Textures.DefaultTexture;
            var Textures = p.Textures.FaceTextures;
            var snap = new PrimSnapshot()
            {
                ID = p.ID,
                Type = p.Type,
                Position = t.WorldPosition,
                Rotation = t.WorldRotation,
                Scale = t.Scale,
                MeshData = p.PrimData,
                MeshID = p.Sculpt?.SculptTexture ?? UUID.Zero,
                DefaultTexture = p.Textures.DefaultTexture,
                Textures = p.Textures.FaceTextures
            };
            list.Add(snap);
        }

        var prims = list.ToArray();

        var terrain = SLManager.Instance.Terrain.GetTerrainHeightmapCopy();

        var snapshot = new Snapshot()
        {
            Primitives = prims,
            Terrain = terrain
        };
        using var fStream = new FileStream(Path, FileMode.OpenOrCreate);
        using var writer = new BinaryWriter(fStream);
        var serializer = new SnapshotSerializer();
        serializer.Write(writer,snapshot);
        Debug.Log("Save Successful");

    }

    void LoadSnap()
    {
        var textureBundleCache = new AssetBundleDiskCache<UUID, Texture>(
            "SLProtoAssets/Texture", 
            (uuid) => uuid.ToString(),
            (uuid) => uuid.ToString()
        );
        var meshBundleCache = new AssetBundleDiskCache<UUID, Mesh>(
            "SLProtoAssets/Mesh", 
            (uuid) => uuid.ToString(),
            (uuid) => uuid.ToString()
        );
        var textureCache = new Dictionary<UUID, Texture>();
        var meshCache = new Dictionary<UUID, Mesh>();
        using var fStream = new FileStream(Path, FileMode.Open);
        using var reader = new BinaryReader(fStream);
        var serializer = new SnapshotSerializer();
        var snapshot = serializer.Read(reader);
        //Primitive Block
        var container = new GameObject("Primitives").transform;
        foreach (var p in snapshot.Primitives)
        {
            var go = new GameObject(
                p.ID.ToString(),
                typeof(MeshFilter),
                typeof(MeshRenderer)
                )
            {
                transform =
                {
                    position = p.Position,
                    rotation = p.Rotation,
                    localScale = p.Scale
                },
            };
            go.transform.SetParent(container,true);

            if (p.Type == PrimType.Mesh)
            {
                var meshFilter = go.GetComponent<MeshFilter>();
                var meshRenderer = go.GetComponent<MeshRenderer>();
                if (p.MeshID.Guid == Guid.Empty) continue;
                Mesh mesh;
                if(meshCache.TryGetValue(p.MeshID, out mesh))
                {
                    // Done
                }
                else if (meshBundleCache.Load(p.MeshID, out mesh))
                {
                    //Copy to preserve references
                    var tempMesh = UMeshData.FromMesh(mesh);
                    mesh = tempMesh.ToUnity();
                }
                else
                {
                    continue;
                }
                
                meshFilter.mesh = mesh;
                UpdateMaterials(textureCache, meshRenderer, p, mesh, textureBundleCache);
            }
            else if(p.Type == PrimType.Sculpt)
                continue;
            else if (p.Type == PrimType.Unknown)
                continue;
            else
            {
                var fakePrim = new Primitive()
                {
                    PrimData = p.MeshData,
                    Textures = new Primitive.TextureEntry(new Primitive.TextureEntryFace(default))
                    {
                        DefaultTexture = new Primitive.TextureEntryFace(default),
                        FaceTextures = new Primitive.TextureEntryFace[50],
                    }
                };
                var meshFilter = go.GetComponent<MeshFilter>();
                var meshRenderer = go.GetComponent<MeshRenderer>();
                var mesher = new MeshmerizerR();
                var slMesh = mesher.GenerateFacetedMesh(fakePrim, DetailLevel.Highest);
                var uMesh = UMeshData.FromSL(slMesh);
                var mesh = uMesh.ToUnity();
                mesh.name = $"Generated `{p.Type}` `{p.MeshData.GetHashCode()}`";
                mesh.Optimize();
                meshFilter.mesh = mesh;
                UpdateMaterials(textureCache, meshRenderer, p, mesh, textureBundleCache);
            }
        }
        //Terrain Block
        var terrain = new GameObject("Terrain", typeof(MeshFilter), typeof(MeshRenderer));
        var terrainGen = new TerrainMeshGenerator();
        var terrainMesh = terrainGen.GenerateMesh(snapshot.Terrain, Vector2.zero, Vector2.one * 255);
        terrain.GetComponent<MeshFilter>().mesh = terrainMesh;
        terrain.GetComponent<MeshRenderer>().material = new Material(SlMaterialUtil.TerrainShader);

    }

    void UpdateMaterials(Dictionary<UUID,Texture> textureCache, MeshRenderer meshRenderer, PrimSnapshot p, Mesh mesh, AssetBundleDiskCache<UUID,Texture> bundleCache)
    {

        // var meshRenderer = GetComponent<MeshRenderer>();
        var materials = new Material[mesh.subMeshCount];
        for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
        {
            var t = p.Textures[subMesh] ?? p.DefaultTexture;
            bool hasAlpha;

            Texture tex;
            if (textureCache.TryGetValue(t.TextureID, out tex))
            {
                hasAlpha = tex != null && ((Texture2D)tex).format == TextureFormat.DXT5 || t.RGBA.A < 1f;
            }
            else if (bundleCache.Load(t.TextureID, out tex))
            {
                hasAlpha = tex != null && ((Texture2D)tex).format == TextureFormat.DXT5 || t.RGBA.A < 1f;
                var tempTex = new Texture2D(tex.width, tex.height, tex.graphicsFormat, TextureCreationFlags.None);
                Graphics.CopyTexture(tex,tempTex);
                tex = tempTex;
                textureCache[t.TextureID] = tex;
            }
            else
            {
                tex = null;
                hasAlpha = t.RGBA.A < 1f;
                textureCache[t.TextureID] = null;
            }

            materials[subMesh] = SlMaterialUtil.CreateMaterial(
                hasAlpha,
                tex,
                t.RGBA.CastUnity(),
                new Vector2(t.OffsetU,t.OffsetV),
                new Vector2(t.RepeatU,t.RepeatV),
                t.Rotation,
                SlMaterialUtil.Shiny2Smooth(t.Shiny)
            );
        }
        meshRenderer.materials = materials;
    }
    
    public static class SlMaterialUtil
    {
        
        public static readonly int BaseMap = Shader.PropertyToID("_AlbedoTex");
        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        public static readonly int Offset = Shader.PropertyToID("_Offset");
        public static readonly int Repeat = Shader.PropertyToID("_Repeat");
        public static readonly int Rotation = Shader.PropertyToID("_Rotation");
        public static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        // private static Shader SimpleShader;
    
        private static readonly string SimpleShaderName = "Shader Graphs/SimpleSL";
        private static Shader _simpleShader;
    
        private static readonly string AlphaShaderName = "Shader Graphs/AlphaSL";
        private static Shader _alphaShader;
        private static readonly string TerrainShaderName = "Shader Graphs/Terrain";
        private static Shader _terrainShader;

        private static Shader GetShader(string name, ref Shader shader)
        {
        
            if (shader != null) return shader;
            
            shader = Shader.Find(name);
            
            if (shader != null) return shader;
            
            throw new NullReferenceException(name);
        }
        public static Shader SimpleShader => GetShader(SimpleShaderName, ref _simpleShader);
        public static Shader AlphaShader => GetShader(AlphaShaderName, ref _alphaShader);
        public static Shader TerrainShader => GetShader(TerrainShaderName, ref _terrainShader);
        public static Shader GetShader(bool alpha) => alpha ? AlphaShader : SimpleShader;

        public static Material CreateMaterial(bool hasAlpha, Texture texture, Color color, Vector2 offset, Vector2 repeat, float rotation, float smoothness)
        {
            var shader = GetShader(hasAlpha);
            var mat = new Material(shader);
            mat.SetTexture(BaseMap,texture);
            mat.SetColor(BaseColor, color);
            mat.SetVector(Offset, offset);
            mat.SetVector(Repeat, repeat);
            mat.SetFloat(Rotation, rotation);
            mat.SetFloat(Smoothness, smoothness);
            return mat;
        }

        public static float Shiny2Smooth(Shininess shiny)
        {
            return shiny switch
            {
                Shininess.None => 0f,
                Shininess.Low => 1f/3f,
                Shininess.Medium => 2f/3f,
                Shininess.High => 1f,
                _ => throw new ArgumentOutOfRangeException(nameof(shiny), shiny, null)
            };
        }
    }
}
