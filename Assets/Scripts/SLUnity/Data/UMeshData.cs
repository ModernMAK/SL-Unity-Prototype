using System;
using System.Collections.Generic;
using System.IO;
using OpenMetaverse.Rendering;
using SLUnity.Serialization;
using UnityEngine;
using Mesh = UnityEngine.Mesh;

namespace SLUnity.Data
{
    public class UMeshData
    {
        public  class Serializer : ISerializer<UMeshData>
        {
            //To cache assets we'd preferably save the Mesh directly using Unity's ScriptableObjects
            //  But we cant serialize them at runtime.
            //
            private const ushort VERSION = 1;
            public  UMeshData Read(BinaryReader reader)
            {
                var version = reader.ReadUInt16();
                if (version != VERSION)
                    throw new Exception();
                var positions = reader.ReadArray(reader.ReadVector3);
                var normals = reader.ReadArray(reader.ReadVector3);
                var texCoords = reader.ReadArray(reader.ReadVector2);
                var indexes = reader.ReadArray(()=>reader.ReadArray(reader.ReadInt32));
                return new UMeshData()
                {
                    Positions = positions,
                    Indexes = indexes,
                    Normals = normals,
                    TexCoord = texCoords
                };
            }
            public  void Write(BinaryWriter writer, UMeshData data)
            {
                writer.Write(VERSION);
                writer.WriteArray(data.Positions,writer.Write);
                writer.WriteArray(data.Normals,writer.Write);
                writer.WriteArray(data.TexCoord,writer.Write);
                writer.WriteArray(data.Indexes,(innerArr)=>writer.WriteArray(innerArr, writer.Write));
            }
        }
        public Vector3[] Positions { get; private set; }
        public Vector3[] Normals { get; private set; }
        public Vector2[] TexCoord { get; private set; }
        public int[][] Indexes { get; private set; }

        public UMeshData(Vector3[] positions, Vector3[] normals, Vector2[] texCoord, int[][] indexes)
        {
            Positions = positions;
            Normals = normals;
            TexCoord = texCoord;
            Indexes = indexes;
        }
        private UMeshData() : this(null,null,null,null){}

        public static UMeshData FromSL(FacetedMesh slMesh)
        {
            
            var vertList = new List<Vector3>();
            var normList = new List<Vector3>();
            var uvList = new List<Vector2>();
            var subMeshTriangleIndexes = new List<int>[slMesh.Faces.Count];
            
            for (var faceIndex = 0; faceIndex < slMesh.Faces.Count; faceIndex++)
            {
                var slF = slMesh.Faces[faceIndex];
                var indList = subMeshTriangleIndexes[faceIndex] = new List<int>(slF.Vertices.Count); // One per vertex (at least).
                var vOffset = vertList.Count; // Single buffer
                foreach (var v in slF.Vertices)
                {
                    vertList.Add(CommonConversion.CoordToUnity(v.Position));
                    normList.Add(CommonConversion.CoordToUnity(v.Normal));
                    uvList.Add(CommonConversion.UVToUnity(v.TexCoord));
                }

                foreach (var i in slF.Indices)
                {
                    indList.Add(vOffset + i);
                }
                indList.Reverse(); // CCW => CW winding order
            }

            var result = new UMeshData()
            {
                Indexes = new int[slMesh.Faces.Count][],
                Normals = normList.ToArray(),
                Positions = vertList.ToArray(),
                TexCoord = uvList.ToArray()
            };
            for (var i = 0; i < slMesh.Faces.Count; i++)
                result.Indexes[i] = subMeshTriangleIndexes[i].ToArray();
            return result;
        }

        public Mesh ToUnity(Mesh mesh = null)
        {
            var m = (mesh != null) ? mesh : new Mesh();
            m.SetVertices(Positions);
            m.SetNormals(Normals);
            m.SetUVs(0,TexCoord);
            m.subMeshCount = Indexes.Length;
            for(var subMesh = 0; subMesh < Indexes.Length; subMesh++)
                m.SetTriangles(Indexes[subMesh],subMesh);
            return m;
        }


        public static UMeshData FromMesh(Mesh mesh)
        {
            var data = new UMeshData();
            data.Positions = mesh.vertices;
            data.Normals = mesh.normals;
            data.TexCoord = mesh.uv;
            var submeshes = mesh.subMeshCount;
            data.Indexes = new int[submeshes][];
            for (var submesh = 0; submesh < submeshes; submesh++)
                data.Indexes[submesh] = mesh.GetTriangles(submesh);
            return data;
        }
    }
}