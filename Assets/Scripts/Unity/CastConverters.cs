//using OpenJpegDotNet.IO;
using OpenMetaverse.Imaging;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
// using FreeImageApi;
using Vector4 = UnityEngine.Vector4;

public static class CastConverters
{
    public static Color CastUnity(this OpenMetaverse.Color4 c) => new Color(c.R,c.G,c.B,c.A);
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