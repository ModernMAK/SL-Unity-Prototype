using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OpenMetaverse;
using OpenMetaverse.Assets;
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
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _client = new HttpClient(handler);
        }


        public async Task Download(Uri uri, Action<byte[]> callback)
        {
            var request = await _client.GetAsync(uri);
            var data = await request.Content.ReadAsByteArrayAsync();
            callback(data);
        }
        
        public IEnumerator DownloadMesh(UUID meshId, Action<AssetMesh> callback)
        {
            void Callback(byte[] meshData)
            {
                var assetMesh = new AssetMesh(meshId,meshData);
                callback(assetMesh);
            }
            var uri = GetMeshURI(meshId);
            var request = Download(uri, Callback);
            request.Start();

            // var request = UnityWebRequest.Get(uri);
            // var downloader = new DownloadHandlerBuffer();
            // request.downloadHandler = downloader;
            // yield return request.SendWebRequest();
            // switch (request.result)
            // {
            //     case UnityWebRequest.Result.InProgress:
            //         throw new Exception($"Connection State Invalid:\n\tIn Progress");
            //     case UnityWebRequest.Result.Success:
            //         var assetMesh = new AssetMesh(meshId, downloader.data);
            //         callback(assetMesh);
            //         yield break;
            //     case UnityWebRequest.Result.ConnectionError:
            //         throw new Exception($"Connection Error:\n{request.error}\n");
            //     case UnityWebRequest.Result.ProtocolError:
            //         throw new Exception($"Protocol Error:\n{request.error}");
            //     case UnityWebRequest.Result.DataProcessingError:
            //         throw new Exception($"Data Processing Error:\n{request.error}");
            //     default:
            //         throw new ArgumentOutOfRangeException();
            // }
        }
        
        public IEnumerator DownloadTexture(UUID textureId, Action<AssetTexture> callback)
        {
            void Callback(byte[] textureData)
            {
                var assetTexture = new AssetTexture(textureId,textureData);
                callback(assetTexture);
            }
            var uri = GetTextureURI(textureId);
            var request = Download(uri, Callback);
            request.Start();
            
            // var uri = GetTextureURI(textureId);
            // var request = UnityWebRequest.Get(uri);
            // var downloader = new DownloadHandlerBuffer();
            // request.downloadHandler = downloader;
            // yield return request.SendWebRequest();
            // switch (request.result)
            // {
            //     case UnityWebRequest.Result.InProgress:
            //         throw new Exception($"Connection State Invalid:\n\tIn Progress");
            //     case UnityWebRequest.Result.Success:
            //         var assetTexture = new AssetTexture(textureId, downloader.data);
            //         callback(assetTexture);
            //         yield break;
            //     case UnityWebRequest.Result.ConnectionError:
            //         throw new Exception($"Connection Error:\n{request.error}");
            //     case UnityWebRequest.Result.ProtocolError:
            //         throw new Exception($"Protocol Error:\n{request.error}");
            //     case UnityWebRequest.Result.DataProcessingError:
            //         throw new Exception($"Data Processing Error:\n{request.error}");
            //     default:
            //         throw new ArgumentOutOfRangeException();
            // }
        }
    }
}