using System;
using System.Collections.Generic;
using System.IO;
//using OpenJpegDotNet.IO;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Imaging;
using OpenMetaverse.Rendering;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
// using FreeImageApi;
using FreeImageAPI;
using UnityTemplateProjects.Unity;
using Vector4 = UnityEngine.Vector4;

public static class CoordConverter
{
    public abstract class ConverterLogic
    {
        // NOTE; THIS SHOULD NEVER CHANGE THE X/Y/Z VALUES, ONLY THEIR ORDER AND SIGN
        public abstract Vector3 SLToUnity(Vector3 sl);
        
        // NOTE; THIS SHOULD NEVER CHANGE THE X/Y/Z VALUES, ONLY THEIR ORDER AND SIGN
        public abstract Vector3 UnityToSL(Vector3 unity);
        
        /// <summary>
        /// Numbers of axis swaps that are performed.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Needed to perform handedness fixes on Quaternions' Angle (w) component.
        /// </remarks>
        public abstract int Swaps { get; }

        private static Quaternion FixQuaternion(Quaternion input, Func<Vector3, Vector3> fixAxis, int swaps)
        {
            
            var axis = new Vector3(input.x, input.y, input.z); // The Axis part of the quaternion
            var angle = input.w; // The Angle part of the quaternion

            var fixedAxis = fixAxis(axis);
            float fixedAngle;
            if (swaps % 2 == 1) 
                fixedAngle = -angle; // Flip handedness
            else
                fixedAngle = angle;
            
            return new Quaternion(fixedAxis.x, fixedAxis.y, fixedAxis.z, fixedAngle);
        }

        public Quaternion SLToUnity(Quaternion sl) => FixQuaternion(sl, SLToUnity, Swaps);
        public Quaternion UnityToSL(Quaternion unity) => FixQuaternion(unity, UnityToSL, Swaps);
    }
    private class NoneConverter : ConverterLogic
    {
        //WORKING UNDER THESE ASSUMPTIONS
        //SL uses : X (-L/R+), Y (-D,U+), Z (-B,F+)
        //Unity uses : X (-L/R+), Y (-D,U+), Z (-B,F+)

        // Unity.X = SL.X
        // Unity.Y = SL.Z
        // Unity.Z = SL.Y
        public override Vector3 SLToUnity(Vector3 sl) => sl;

        // SL.X = Unity.X
        // SL.Y = Unity.Z
        // SL.Z = Unity.Y
        public override Vector3 UnityToSL(Vector3 unity) => unity;

        public override int Swaps => 0;
    }
    private class MAKConverter : ConverterLogic
    {
        //WORKING UNDER THESE ASSUMPTIONS
        //SL uses : X (-L/R+), Y (-B/F+), Z (-D/U+)
        //Unity uses : X (-L/R+), Y (-D,U+), Z (-B,F+)

        // Unity.X = SL.X
        // Unity.Y = SL.Z
        // Unity.Z = SL.Y
        public override Vector3 SLToUnity(Vector3 sl) => new Vector3(sl.x, sl.z, sl.y);

        // SL.X = Unity.X
        // SL.Y = Unity.Z
        // SL.Z = Unity.Y
        public override Vector3 UnityToSL(Vector3 unity) => new Vector3(unity.x, unity.z, unity.y);

        // 1 Swaps total; Handedness changed (Odd # of Swaps)
        public override int Swaps => 1;
    }
    private class CatnipConverter : ConverterLogic
    {
        //WORKING UNDER THESE ASSUMPTIONS (suggested by alexis catnip)
        //SL uses : X (-B/F+), Y (-R/L+), Z (-D/U+)
        //Unity uses : X (-L/R+), Y (-D,U+), Z (-B,F+)
        
        // Unity.X = -SL.Y
        // Unity.Y = SL.Z
        // Unity.Z = SL.X
        public override Vector3 SLToUnity(Vector3 sl) => new Vector3(-sl.y, sl.z, sl.x);

        // SL.X = Unity.Z 
        // SL.Y = -Unity.X
        // SL.Z = Unity.Y
        public override Vector3 UnityToSL(Vector3 unity) => new Vector3(unity.z, -unity.x, unity.y);

        // X => Z, Y => X, and the negation of X; 3 swaps
        // X, Y, Z => Z, Y, X (1 Swap; X swaps with Z)
        // Z, Y, X => Z, X, Y (1 Swap; X swaps with Y)
        // Z, X, Y => Z, -X, Y (1 Swap; X swaps with -X)
        // 3 Swaps total; Handedness changed (Odd # of Swaps)
        public override int Swaps => 3; 
    }

    private static readonly ConverterLogic None = new NoneConverter();
    private static readonly ConverterLogic MAK = new MAKConverter();
    private static readonly ConverterLogic CATNIP = new CatnipConverter();
    
    public enum ConverterMode
    {
        None,
        MAK, CATNIP
    }

    public static ConverterMode Mode = ConverterMode.MAK; 
    public static ConverterLogic Converter
    {
        get
        {
            switch (Mode)
            {
                case ConverterMode.None:
                    return None;
                case ConverterMode.MAK:
                    return MAK;
                case ConverterMode.CATNIP:
                    return CATNIP;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

}

public static class CastConverters
{
    public static Vector2 CastUnity(this OpenMetaverse.Vector2 v) => new Vector2(v.X, v.Y);
    public static Vector3 CastUnity(this OpenMetaverse.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
    public static Vector3 CastUnity(this OpenMetaverse.Vector3d v) => new Vector3((float)v.X, (float)v.Y, (float)v.Z);
    public static Vector4 CastUnity(this OpenMetaverse.Vector4 v) => new Vector4(v.X, v.Y, v.Z, v.W);
    public static Quaternion CastUnity(this OpenMetaverse.Quaternion v) => new Quaternion(v.X, v.Y, v.Z, v.W);
    
    public static OpenMetaverse.Vector2 CastSL(this Vector2 v) => new OpenMetaverse.Vector2(v.x, v.y);
    public static OpenMetaverse.Vector3 CastSL(this Vector3 v) => new OpenMetaverse.Vector3(v.x, v.y, v.z);
    public static OpenMetaverse.Vector4 CastSL(this Vector4 v) => new OpenMetaverse.Vector4(v.x, v.y, v.z, v.w);
    public static OpenMetaverse.Quaternion CastSL(this Quaternion v) => new OpenMetaverse.Quaternion(v.x, v.y, v.z, v.w);
}


public static class CommonConversion
{
    public static Vector3 CoordToUnity(OpenMetaverse.Vector3 v) => CoordConverter.Converter.SLToUnity(v.CastUnity());
    public static Vector2 UVToUnity(OpenMetaverse.Vector2 v) => v.CastUnity();

    public static Quaternion RotToUnity(OpenMetaverse.Quaternion q) =>
        CoordConverter.Converter.SLToUnity(q.CastUnity());
}
public class UTexture
{
    public static class Serializer
    {
        public static UTexture Read(BinaryReader reader)
        {
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var pixels = reader.ReadByteArray();
            return new UTexture(width, height, pixels);
        }
        public static void Write(BinaryWriter writer, UTexture texture)
        {
            writer.Write(texture.Width);
            writer.Write(texture.Height);
            writer.WriteByteArray(texture.Data);
        }

    }

    public UTexture(int width, int height, byte[] pixels)
    {
        Width = width;
        Height = height;
        Data = pixels;
    }
    
    public int Width { get; private set; }
    public int Height { get; private set; }
    public byte[] Data { get; private set; }

    public static UTexture FromSL(AssetTexture assetTexture)
    {
            
        using var inStream = new MemoryStream(assetTexture.AssetData);
        var bitmap = FreeImage.LoadFromStream(inStream);
        using var outStream = new MemoryStream();
        if (!FreeImage.SaveToStream(bitmap, outStream, FREE_IMAGE_FORMAT.FIF_PNG))
            throw new InvalidOperationException("Image failed to convert (JP2->PNG)!");
        var w = FreeImage.GetWidth(bitmap);
        var h = FreeImage.GetHeight(bitmap);
        return new UTexture((int)w, (int)h, outStream.GetBuffer());

    }

    public Texture2D ToUnity()
    {
        var tex = new Texture2D(Width, Height);
        if(!tex.LoadImage(Data))
            throw new InvalidOperationException("Image failed to convert (PNG->Tex2D)!");
        return tex;
    }

}
public static class SLMeshUtil
{
    private static readonly DetailLevel[] DetailLevels = new []
        { DetailLevel.Highest, DetailLevel.High, DetailLevel.Medium, DetailLevel.Low };

    public static IEnumerable<DetailLevel> DetailHi2Lo
    {
        get
        {
            for (var i = 0; i < DetailLevels.Length; i++)
                yield return DetailLevels[i];
        }
    }
    public static IEnumerable<DetailLevel> DetailLo2Hi
    {
        get
        {
            for (var i = DetailLevels.Length-1; i >= 0; i--)
                yield return DetailLevels[i];
        }
    }

    public static bool TryDecodeHighestLOD(Primitive prim, AssetMesh assetMesh, out DetailLevel LOD, out FacetedMesh mesh)
    {
        foreach (var lod in DetailHi2Lo)
        {
            if (!FacetedMesh.TryDecodeFromAsset(prim, assetMesh, lod, out mesh)) continue;
            LOD = lod;
            return true;
        }

        LOD = default;
        mesh = default;
        return false;

    }
}