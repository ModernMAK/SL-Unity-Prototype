using System;
using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Imaging;
using OpenMetaverse.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Mesh = UnityEngine.Mesh;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public static class SLUnityConversions
{
    public static Vector2 ToUnity(this OpenMetaverse.Vector2 v) => new Vector2(v.X, v.Y);
    //SL uses : X (-L/R+), Y (-B/F+), Z (-D/U+)
    public static Vector3 ToUnity(this OpenMetaverse.Vector3 v, bool fixAxis = true) => fixAxis ? new Vector3(v.X, v.Z, v.Y) : new Vector3(v.X, v.Y, v.Z); 
    public static Vector3 ToUnity(this OpenMetaverse.Vector3d v, bool fixAxis = true) => fixAxis ? new Vector3((float)v.X, (float)v.Z, (float)v.Y) : new Vector3((float)v.X, (float)v.Y, (float)v.Z); 
    /// <summary>
    /// 
    /// </summary>
    /// <param name="q">The quaternion to convert.</param>
    /// <param name="upIsZ">Whether the z component of the source quaternion should be treated as up (this will swap the y and z components).</param>
    /// <returns></returns>
    /// <remarks>
    /// An explanation for why we can just swap Y/Z in a quaternion.
    /// Quaternions are a very special form of axis-angle rotations. As long as we are ONLY reassigning axis, we can swap (and even negate them) freely.
    /// BUT, the angle (for math reasons) must be negated every time we perform a single swap/negation.
    /// Since we only swap Y & Z, we only need to negate W (the angle) once.
    ///
    /// BUT, we also need to flip handedness, so we 
    /// </remarks>
    public static Quaternion ToUnity(this OpenMetaverse.Quaternion q, bool upIsZ = true) => upIsZ ? new Quaternion(q.X, q.Z, q.Y, -q.W) : new Quaternion(q.X, q.Y, q.Z, q.W);

    public static Mesh ToUnity(this OpenMetaverse.Rendering.FacetedMesh slMesh)
    {
        var uMesh = new Mesh();
        var vertList = new List<Vector3>(slMesh.Faces.Count * 3); // Assume at least that many triangles
        var normList = new List<Vector3>(slMesh.Faces.Count * 3); // Assume at least that many triangles
        var uvList = new List<Vector2>(slMesh.Faces.Count * 3); // Assume at least that many triangles
        var subMeshTriangleIndexes = new List<int>[slMesh.Faces.Count]; //Assume each face is a submesh?

        for (var faceIndex = 0; faceIndex < slMesh.Faces.Count; faceIndex++)
        {
            var slF = slMesh.Faces[faceIndex];
            var indList =
                subMeshTriangleIndexes[faceIndex] = new List<int>(slF.Vertices.Count); // One per vertex (at least).
            var vOffset = vertList.Count; // Single buffer
            foreach (var v in slF.Vertices)
            {
                vertList.Add(v.Position.ToUnity());
                normList.Add(v.Normal.ToUnity());
                uvList.Add(v.TexCoord.ToUnity());
            }

            foreach (var i in slF.Indices)
            {
                indList.Add(vOffset + i);
            }
            indList.Reverse(); // CCW => CW winding order
        }

        const MeshUpdateFlags dontUpdate = MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers |
                                           MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds;
        const int textureUvs = 0;
        uMesh.subMeshCount = subMeshTriangleIndexes.Length;
        uMesh.SetVertices(vertList, 0, vertList.Count, dontUpdate);
        uMesh.SetNormals(normList, 0, normList.Count, dontUpdate);
        uMesh.SetUVs(textureUvs, uvList, 0, uvList.Count, dontUpdate);
        for (var subMeshIndex = 0; subMeshIndex < subMeshTriangleIndexes.Length; subMeshIndex++)
        {
            var triangleList = subMeshTriangleIndexes[subMeshIndex];
            uMesh.SetTriangles(triangleList,subMeshIndex,true);
        }
        uMesh.RecalculateBounds();
        uMesh.RecalculateTangents();
        uMesh.Optimize();
        return uMesh;
    }

    public static Texture ToUnity(this AssetTexture assetTexture)
    {
        var texture = new Texture2D(8, 8);
        if (texture.LoadImage(assetTexture.AssetData))
            return texture;
        throw new Exception("Failed to produce a texture!");
        //     if (!assetTexture.Decode())
        //         throw new Exception("Decode texture failed!");
        //     var img = assetTexture.Image;
        //     
        //     if (img.Channels == ManagedImage.ImageChannels.Color)
        //     {
        //         var texture = new Texture2D(img.Width,img.Height,TextureFormat.RGBA32,true);
        //         var colors = new Color32[img.Width * img.Height];
        //         for (var y = 0; y < img.Height; y++)
        //         for(var x = 0; x < img.Width; x++)
        //         {
        //             var i = y * img.Width + x;
        //             colors[i] = new Color(img.Red[i], img.Green[i], img.Blue[i], img.Alpha[i]);
        //         }
        //         texture.SetPixels32(colors);
        //         return texture;
        //     }
        //     else
        //     {
        //         throw new NotSupportedException();
        //     }
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