using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OpenMetaverse;
using OpenMetaverse.Assets;
using UnityEngine;
using UnityEngine.Networking;

namespace SLUnity
{
    public class DownloadManager
    {
        public const string CDN = "http://asset-cdn.glb.agni.lindenlab.com";
        public const string CDN_TEX = CDN + "/?texture_id=";// UUID
        public const string CDN_MESH = CDN + "/?mesh_id=";// UUID

        public static Uri GetTextureURI(UUID textureId) => new Uri(CDN_TEX + textureId.ToString());
        public static Uri GetMeshURI(UUID meshId) => new Uri(CDN_MESH + meshId.ToString());
        
        //Dont use the absolute garbage fire that is UnityWebRequest;
        //  Rather than speweing NotSupportedErrors for stuff that isn't supported cross-platform, they just limited everything you could do with it

        private readonly HttpClient _client;

        public DownloadManager()
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };
            _client = new HttpClient(handler);
        }


        public async Task DownloadAsync(Uri uri, Action<byte[]> callback)
        {
            var request = await _client.GetAsync(uri);
            if (!request.IsSuccessStatusCode)
            {
                callback(null);
            }
            var data = await request.Content.ReadAsByteArrayAsync();
            callback(data);
        }

        public void Download(Uri uri, Action<byte[]> callback)
        {
            var request = DownloadAsync(uri, callback);
            if (request.Status == TaskStatus.Created)
                request.Start();
        }
        
        public void DownloadMesh(UUID meshId, Action<AssetMesh> callback)
        {
            void Callback(byte[] meshData)
            {
                var assetMesh = meshData != null ? new AssetMesh(meshId,meshData) : null;
                callback(assetMesh);
            }
            var uri = GetMeshURI(meshId);
            Download(uri,Callback);

        }
        
        public void DownloadTexture(UUID textureId, Action<AssetTexture> callback)
        {
            void Callback(byte[] textureData)
            {
                var assetTexture = textureData != null ? new AssetTexture(textureId,textureData) : null;
                callback(assetTexture);
            }
            var uri = GetTextureURI(textureId);
            Download(uri,Callback);
            
        }
    }
}