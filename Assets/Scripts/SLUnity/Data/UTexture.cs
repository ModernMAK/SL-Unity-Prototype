using System;
using System.IO;
using FreeImageAPI;
using OpenMetaverse.Assets;
using SLUnity.Serialization;
using UnityEngine;

namespace SLUnity.Data
{
    
    public class UTexture
{
    public class Serializer : ISerializer<UTexture>
    {
        private const ushort VERSION = 2;
        public UTexture Read(BinaryReader reader)
        {
            var version = reader.ReadUInt16();
            if (version != VERSION)
                throw new Exception();
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

    public static UTexture FromSL(AssetTexture assetTexture)
    {
        // var hasAlpha = assetTexture.Components == 4;
        using var inStream = new MemoryStream(assetTexture.AssetData);
        var bitmap = FreeImage.LoadFromStream(inStream);
        if (bitmap == null)
            throw new NullReferenceException("Bitmap is null!");
        var hasAlpha = FreeImage.IsTransparent(bitmap);
        // var rgb = FreeImage.GetChannel(bitmap,FREE_IMAGE_COLOR_CHANNEL.FICC_RGB);
        using var outStream = new MemoryStream();
        if (!FreeImage.SaveToStream(bitmap, outStream, FREE_IMAGE_FORMAT.FIF_PNG))
            throw new InvalidOperationException("Image failed to convert (JP2->PNG)!");
        var w = FreeImage.GetWidth(bitmap);
        var h = FreeImage.GetHeight(bitmap);
        return new UTexture((int)w, (int)h, outStream.GetBuffer(), hasAlpha);

    }

    public Texture2D ToUnity()
    {
        // Dont generate mips ~ Assume Alpha (since LoadImage will set it to RGBA32 anyways)
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
