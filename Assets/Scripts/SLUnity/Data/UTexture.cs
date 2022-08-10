using System;
using System.IO;
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
        private const ushort VERSION = 3;
        public UTexture Read(BinaryReader reader)
        {
            var version = reader.ReadUInt16();
            switch (version)
            {
                case VERSION:
                    break; //
                case 2:
                    throw new Exception("Version not supported: UTexture no longer stores data as a PNG!");
                default:
                    throw new Exception("Version not supported!");
                
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

    [Obsolete]
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
        var format = HasAlpha ? TextureFormat.DXT5 : TextureFormat.DXT1;
        var tex = new Texture2D(Width, Height, format,false);
        tex.LoadRawTextureData(Data);
        return tex;
    }
    
}
}
