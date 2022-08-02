using System;
using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;
using OpenMetaverse.StructuredData;
using SLUnity.Data;
using Mesh = UnityEngine.Mesh;
using Vector3 = UnityEngine.Vector3;
using Vector2 = UnityEngine.Vector2;

namespace SLUnity.Rendering
{
    public interface IMeshGenerator
    {
        UMeshData GenerateMeshData(AssetMesh meshAsset, DetailLevel LOD = DetailLevel.Highest);
        Mesh GenerateMesh(AssetMesh meshAsset, DetailLevel LOD = DetailLevel.Highest);
    }

    public class MeshGenerator : IMeshGenerator
    {
        private static Vector3 UnpackVector3(OSD osd)
        {
            
            OpenMetaverse.Vector3 v = osd;
            return v.CastUnity();
        }

        private static Vector2 UnpackVector2(OSD osd)
        {
            OpenMetaverse.Vector2 v = osd;
            return v.CastUnity();
        }

        // Unity.X = SL.X
        // Unity.Y = SL.Z
        // Unity.Z = SL.Y
        private static Vector3 Assemble(float x, float y, float z) => new Vector3(x,z,y);
        //Vector2 doesn't require Remapping
        private static Vector2 Assemble(float x, float y) => new Vector2(x,y); 

        private UMeshData GenerateMeshData(OSDArray decodedMeshOsdArray)
        {
            
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var texcoords = new List<Vector2>();
            var indexes = new List<int>[decodedMeshOsdArray.Count];

            for (int faceIndex = 0; faceIndex < decodedMeshOsdArray.Count; faceIndex++)
            {
                OSD subMeshOsd = decodedMeshOsdArray[faceIndex];

                // Decode each individual face
                if (subMeshOsd is OSDMap subMeshMap)
                {
                    var localIndexes = indexes[faceIndex] = new List<int>();
                    var vertexOffset = positions.Count;
                    // As per http://wiki.secondlife.com/wiki/Mesh/Mesh_Asset_Format, some Mesh Level
                    // of Detail Blocks (maps) contain just a NoGeometry key to signal there is no
                    // geometry for this submesh.
                    if (subMeshMap.ContainsKey("NoGeometry") && ((OSDBoolean)subMeshMap["NoGeometry"]))
                    {
                        continue;
                    }
                    

                    Vector3 posMax;
                    Vector3 posMin;

                    Vector3 normMax = Vector3.one;
                    Vector3 normMin = -Vector3.one;

                    // If PositionDomain is not specified, the default is from -0.5 to 0.5
                    if (subMeshMap.ContainsKey("PositionDomain"))
                    {
                        posMax = UnpackVector3(((OSDMap)subMeshMap["PositionDomain"])["Max"]);
                        posMin = UnpackVector3(((OSDMap)subMeshMap["PositionDomain"])["Min"]);
                    }
                    else
                    {
                        posMax = new Vector3(0.5f, 0.5f, 0.5f);
                        posMin = new Vector3(-0.5f, -0.5f, -0.5f);
                    }

                    // Vertex positions
                    byte[] posBytes = subMeshMap["Position"];

                    // Normals
                    byte[] norBytes = null;
                    if (subMeshMap.ContainsKey("Normal"))
                    {
                        norBytes = subMeshMap["Normal"];
                    }

                    // UV texture map
                    Vector2 texPosMax = Vector2.zero;
                    Vector2 texPosMin = Vector2.zero;
                    byte[] texBytes = null;
                    if (subMeshMap.ContainsKey("TexCoord0"))
                    {
                        texBytes = subMeshMap["TexCoord0"];
                        texPosMax = UnpackVector2(((OSDMap)subMeshMap["TexCoord0Domain"])["Max"]);
                        texPosMin = UnpackVector2(((OSDMap)subMeshMap["TexCoord0Domain"])["Min"]);
                    }

                    // Extract the vertex position data
                    // If present normals and texture coordinates too
                    var vertexCount = posBytes.Length / 6;
                    for (int vertexIndex = 0; vertexIndex < vertexCount ; vertexIndex ++)
                    {
                        //REMEMBER
                        var vertexByteIndex = vertexIndex * 6;
                        ushort pX = Utils.BytesToUInt16(posBytes, vertexByteIndex);
                        ushort pY = Utils.BytesToUInt16(posBytes, vertexByteIndex + 2);
                        ushort pZ = Utils.BytesToUInt16(posBytes, vertexByteIndex + 4);

                        var position = Assemble(
                            Utils.UInt16ToFloat(pX, posMin.x, posMax.x),
                            Utils.UInt16ToFloat(pY, posMin.y, posMax.y),
                            Utils.UInt16ToFloat(pZ, posMin.z, posMax.z)
                        );
                        positions.Add(position);
                        
                        var normalByteIndex = vertexIndex * 6;
                        if (norBytes != null && norBytes.Length >= normalByteIndex + 6)
                        {
                            ushort nX = Utils.BytesToUInt16(norBytes, normalByteIndex);
                            ushort nY = Utils.BytesToUInt16(norBytes, normalByteIndex + 2);
                            ushort nZ = Utils.BytesToUInt16(norBytes, normalByteIndex + 4);

                            var normal = Assemble(
                                Utils.UInt16ToFloat(nX, normMin.x, normMax.x),
                                Utils.UInt16ToFloat(nY, normMin.y, normMax.y),
                                Utils.UInt16ToFloat(nZ, normMin.z, normMax.z)
                            );
                            normals.Add(normal);
                        }


                        var texByteIndex = vertexIndex * 4;
                        if (texBytes != null && texBytes.Length >= texByteIndex + 4)
                        {
                            ushort tX = Utils.BytesToUInt16(texBytes, vertexIndex);
                            ushort tY = Utils.BytesToUInt16(texBytes, vertexIndex + 2);

                            var texCoord = Assemble(
                                Utils.UInt16ToFloat(tX, texPosMin.x, texPosMax.x),
                                Utils.UInt16ToFloat(tY, texPosMin.y, texPosMax.y)
                            );
                            texcoords.Add(texCoord);
                        }

                    }

                    byte[] triangleBytes = subMeshMap["TriangleList"];
                    for (int i = 0; i < triangleBytes.Length; i += 6)
                    {
                        ushort v1 = Utils.BytesToUInt16(triangleBytes, i);
                        ushort v2 = Utils.BytesToUInt16(triangleBytes, i + 2);
                        ushort v3 = Utils.BytesToUInt16(triangleBytes, i + 4);
                        // Unity and SL use opposite winding orders!
                        // We also need to flatten the array;  so we need to offset by the vertexOffset
                        localIndexes.Add(v3 + vertexOffset);
                        localIndexes.Add(v2 + vertexOffset);
                        localIndexes.Add(v1 + vertexOffset);
                    }
                }
            }
            var flatPos = positions.ToArray();
            var flatNorm = normals.ToArray();
            var flatTex = texcoords.ToArray();
            var flatSubmesh = new int[indexes.Length][];
            for (var submesh = 0; submesh < indexes.Length; submesh++)
                flatSubmesh[submesh] = indexes[submesh].ToArray();
            return new UMeshData(flatPos, flatNorm, flatTex, flatSubmesh);
        }
        public UMeshData GenerateMeshData(AssetMesh meshAsset, DetailLevel LOD = DetailLevel.Highest)
        {
            if (!meshAsset.Decode())
                throw new Exception("Mesh could not be decoded!");
            OSDMap data = meshAsset.MeshData;

            OSD facesOSD;

            switch (LOD)
            {
                case DetailLevel.Highest:
                    facesOSD = data["high_lod"];
                    break;

                case DetailLevel.High:
                    facesOSD = data["medium_lod"];
                    break;

                case DetailLevel.Medium:
                    facesOSD = data["low_lod"];
                    break;

                case DetailLevel.Low:
                    facesOSD = data["lowest_lod"];
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(LOD));
            }

            if (!(facesOSD is OSDArray decodedMeshOsdArray))
                throw new Exception("Face is not OSDArray!");

            return GenerateMeshData(decodedMeshOsdArray);

        }

        public Mesh GenerateMesh(AssetMesh meshAsset, DetailLevel LOD = DetailLevel.Highest) =>
            GenerateMeshData(meshAsset, LOD).ToUnity();

    }
}