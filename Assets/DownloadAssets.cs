using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Attributes;
using FreeImageAPI;
using ImageMagick;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;
using SLUnity.Data;
using SLUnity.Snapshot;
using UnityEngine;
using Path = System.IO.Path;

public class DownloadAssets : MonoBehaviour
{
    public bool LoadTex = true;
    public bool LoadMesh = true;
    
    public string SceneGraph;
    public string AssetCache;

    private int ConcurrentDLS = 4;
    
    [Header("Mesh")]
    [SerializeField]
    [ReadOnly] private int _meshCount;
    [SerializeField]
    [ReadOnly] private int _meshesRequested;
    [SerializeField]
    [ReadOnly] private int _meshesProcessed;
    [Header("Texture")]
    [SerializeField]
    [ReadOnly] private int _textureCount;
    [SerializeField]
    [ReadOnly] private int _textureRequested;
    [SerializeField]
    [ReadOnly] private int _texturesProcessed;

    [Header("Timings")] [SerializeField] [ReadOnly]
    private float _startTime;
    private float _meshStopTime;
    [SerializeField] [ReadOnly]
    private float _meshElapsed;
    private float _texStopTime;
    [SerializeField] [ReadOnly]
    private float _texElapsed;

    private static readonly SLUnity.DownloadManager downloader = new SLUnity.DownloadManager();
    private List<UUID> _meshes;
    private List<UUID> _textures;
    void Awake()
    {
        _meshCount = _meshesRequested = _meshesProcessed = _textureCount = _textureRequested = _texturesProcessed = 0;
        _startTime = Time.time;
        _meshStopTime = _texStopTime = _texElapsed = _meshElapsed = -1;
        try
        {
            Debug.Log("Reading Scene Graph");
            using var fstream = new FileStream(SceneGraph, FileMode.Open);
            using var reader = new BinaryReader(fstream);
            var serilaizer = new SLRegionSerializer();
            var scene = serilaizer.Read(reader);
            var prims = scene.Primitives;

            Debug.Log("Processing Scene Graph");
            HashSet<UUID> meshes = new HashSet<UUID>();
            HashSet<UUID> textures = new HashSet<UUID>();
            foreach (var prim in prims)
            {

                if (prim.Type == PrimType.Mesh && prim.Sculpt.SculptTexture != UUID.Zero)
                {
                    var meshID = prim.Sculpt.SculptTexture;
                    meshes.Add(meshID);
                }

                var defTex = prim.Textures.DefaultTexture;
                var faceTexs = prim.Textures.FaceTextures;
                if (defTex != null && defTex.TextureID != UUID.Zero)
                {
                    textures.Add(defTex.TextureID);
                }

                foreach (var faceTex in faceTexs)
                    if (faceTex != null && faceTex.TextureID != UUID.Zero)
                    {
                        textures.Add(faceTex.TextureID);
                    }
            }

            _textureCount = textures.Count;
            _meshCount = meshes.Count;
            // foreach (var meshID in meshes)
                // downloader.DownloadMesh(meshID, MeshDownloaded);
            // foreach (var texID in textures)
                // downloader.DownloadTexture(texID, TextureDownloaded);

            _meshes = meshes.ToList();
            _textures = textures.ToList();
            Debug.Log("Downloading Assets");
            for (var i = 0; i < ConcurrentDLS; i++)
            {
                DLMesh();
                DLTexture();
            }
        }
        catch (IOException ex)
        {
            Debug.LogException(ex);
        }
    }

    private void DLMesh()
    {
        if(!LoadMesh)
            return;
        if (_meshes.Count <= _meshesRequested)
        {
            _meshStopTime = Time.time;
            _meshElapsed = _meshStopTime - _startTime;
            return;
        }
        var id = _meshes[_meshesRequested];
        Debug.Log($"Downloading [MESH]: `{id.Guid}`");
        downloader.DownloadMesh(id, MeshDownloaded);
        _meshesRequested++;
    }
    private void DLTexture()
    {
        if(!LoadTex)
            return;
        if (_textures.Count <= _textureRequested)
        {
            _texStopTime = Time.time;
            _texElapsed = _texStopTime - _startTime;
            return;
        }
        var id = _textures[_textureRequested];       
        Debug.Log($"Downloading [TEXTURE]: `{id.Guid}`");

        downloader.DownloadTexture(id, TextureDownloaded);
        _textureRequested++;
    }
    private void TextureDownloaded(AssetTexture asset)
    {
        if (asset == null) // NotFound
        {
            DLTexture();
            return;
        }
            
        using var inStream = new MemoryStream(asset.AssetData);
        var bitmap = FreeImage.LoadFromStream(inStream);
        var hasAlpha = FreeImage.IsTransparent(bitmap);
        // var rgb = FreeImage.GetChannel(bitmap,FREE_IMAGE_COLOR_CHANNEL.FICC_RGB);
        using var outStream = new MemoryStream();
        if (!FreeImage.SaveToStream(bitmap, outStream, FREE_IMAGE_FORMAT.FIF_PNG))
            throw new InvalidOperationException("Image failed to convert (JP2->PNG)!");
        var w = FreeImage.GetWidth(bitmap);
        var h = FreeImage.GetHeight(bitmap);
        var utex = new UTexture((int)w, (int)h, outStream.GetBuffer(), hasAlpha);
        //     
        // using var image = new MagickImage(asset.AssetData);
        // var hasAlpha = image.HasAlpha;
        // // var format =
        // image.Format =  hasAlpha ? MagickFormat.Dxt5 : MagickFormat.Dxt1;
        // // image.Compression = hasAlpha ? CompressionMethod.DXT5 : CompressionMethod.DXT1;
        // byte[] dxt;
        // using (var memStream = new MemoryStream())
        // {
        //     image.Write(memStream);
        //     dxt = memStream.ToArray();
        // }
        // var w = image.Width;
        // var h = image.Height;
        // var utex = new UTexture(w, h, dxt, hasAlpha);
        //
        var file = Path.Combine(AssetCache, "Texture", asset.AssetID.Guid.ToString() + ".utex");

        using (var fstream = new FileStream(file, FileMode.OpenOrCreate))
        {
            using var writer = new BinaryWriter(fstream);
            var serializer = new UTexture.Serializer();

            serializer.Write(writer, utex);

            _texturesProcessed++;
            DLTexture();
        }

    }

    private static Primitive NULL_PRIM = new Primitive()
    {
        Textures = new Primitive.TextureEntry(UUID.Zero)
    };
    private void MeshDownloaded(AssetMesh asset)
    {
        if (asset == null) // NotFound
        {
            DLMesh();
            return;
        }
        
        if (!FacetedMesh.TryDecodeFromAsset(NULL_PRIM, asset, DetailLevel.Highest, out var slMesh))
        {
            DLMesh();
            return;
        };
        var meshData = UMeshData.FromSL(slMesh);

        var file = Path.Combine(AssetCache, "Mesh","Highest", asset.AssetID.Guid.ToString() + ".umesh");
        
        using var fstream = new FileStream(file, FileMode.OpenOrCreate);
        using var writer = new BinaryWriter(fstream);
        var serializer = new UMeshData.Serializer();
        serializer.Write(writer,meshData);

        _meshesProcessed++;
        DLMesh();

    }
}
