using System;
using System.IO;
using System.Runtime.Serialization;
using FreeImageAPI;
using OpenMetaverse.Assets;
using SLUnity.Serialization;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace SLUnity.Data
{
    
    public class UTexture
{
    public class Serializer : ISerializer<UTexture>
    {
        private const ushort VERSION = 5;
        public UTexture Read(BinaryReader reader)
        {
            var version = reader.ReadUInt16();
            switch (version)
            {
                case VERSION:
                    break; //
                case 4:
                    throw new Exception($"Version `{version}` not supported: UTexture data is DXT1/DXT5");
                case 3:
                    throw new Exception($"Version `{version}` not supported: UTexture requires format to be DXT1/DXT5");
                case 2:
                    throw new Exception($"Version `{version}` not supported: UTexture no longer stores data as a PNG!");
                default:
                    throw new Exception($"Version `{version}` not supported!");
                
            }

            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var alpha = reader.ReadBoolean();
            var pixels = reader.ReadByteArray();
            return new UTexture(width, height, pixels,alpha);
        }
        public void Write(BinaryWriter writer, UTexture texture)
        {
            writer.Write(VERSION);
            writer.Write(texture.Width);
            writer.Write(texture.Height);
            writer.Write(texture.HasAlpha);
            writer.WriteByteArray(texture.Data);
        }

    }

    public UTexture(int width, int height, byte[] pixels, bool alpha)
    {
        Width = width;
        Height = height;
        Data = pixels;
        HasAlpha = alpha;
    }
    
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool HasAlpha { get; private set; }
    public byte[] Data { get; private set; }



    public Texture2D ToUnity()
    {
        var tex = new Texture2D(Width, Height,TextureFormat.RGBA32,false);
        if(!tex.LoadImage(Data))
            throw new InvalidOperationException("Image failed to convert (PNG->Tex2D)!");
        tex.alphaIsTransparency = HasAlpha;
        if (HasAlpha) return tex; // Done
        
        var srcTex = tex; 
        // Fix TextureFormat so we can use it to check ALPHA
        tex = new Texture2D(Width, Height, TextureFormat.RGB24,false);
        var colors = srcTex.GetPixels32();
        tex.SetPixels32(colors);
        return tex;
    }
    
}
}
