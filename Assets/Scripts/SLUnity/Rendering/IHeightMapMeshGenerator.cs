using SLUnity.Data;
using SLUnity.Managers;
using UnityEngine;

namespace SLUnity.Rendering
{
    public interface IHeightMapMeshGenerator
    {
        UMeshData GenerateMeshData(float[,] yMap, Vector2 min, Vector2 max);
        Mesh GenerateMesh(float[,] yMap, Vector2 min, Vector2 max);
    }

    public class TerrainMeshGenerator : IHeightMapMeshGenerator
    {
        
        public UMeshData GenerateMeshData(float[,] yMap, Vector2 min, Vector2 max)
        {
            var xSize = yMap.GetLength(0);
            var zSize = yMap.GetLength(1);
            int PosToIndex(int2 p) => (p.y * xSize) + p.x;
            int XZToIndex(int x, int z) => (z * xSize) + x;
            bool IndexValid(int i) => (0 <= i && i < (xSize * zSize));
            
            var origin = new Vector3(min.x, 0, min.y);

            var xStep = (max.x - min.x) / (xSize - 1);
            var zStep = (max.y - min.y) / (zSize - 1);
            var uStep = 1.0f / (xSize - 1);
            var vStep = 1.0f / (zSize - 1);


            var flatSize = xSize * zSize;
            var positions = new Vector3[flatSize];
            var normals = new Vector3[flatSize];
            var texcoords = new Vector2[flatSize];
            var indexes = new int[(xSize-1) * (zSize-1) * 2 * 3];
            
            for(var x = 0; x < xSize; x++)
            for (var z = 0; z < zSize; z++)
            {
                positions[XZToIndex(x, z)] = origin + new Vector3(xStep * x, yMap[x, z], zStep * z);
                texcoords[XZToIndex(x, z)] = new Vector2(uStep * x , vStep * z);
            }
            for(var x = 0; x < xSize; x++)
            for (var z = 0; z < zSize; z++)
            {
                var north = new int2(0,1);
                var east = new int2(1,0);
                var self = new int2(x, z);

                var i = PosToIndex(self);
                var nNI = PosToIndex(self + north);
                var nSI = PosToIndex(self - north);
                var nEI = PosToIndex(self + east);
                var nWI = PosToIndex(self - east);

                var nN = positions[IndexValid(nNI) ? nNI : i].y;
                var nS = positions[IndexValid(nSI) ? nSI : i].y;
                var nE = positions[IndexValid(nEI) ? nEI : i].y;
                var nW = positions[IndexValid(nWI) ? nWI : i].y;

                var nX = nW - nE;
                var nZ = nS - nN;

                normals[i] = new Vector3(nX, 2, nZ).normalized;
            }
            
            for(var x = 0; x < xSize-1; x++)
            for (var z = 0; z < zSize - 1; z++)
            {
                var i = (z * (xSize-1) + x);
                var backLeft = XZToIndex(x,z);
                var backRight =  XZToIndex(x+1,z);
                var forwardLeft =  XZToIndex(x,z+1);
                var forwardRight =  XZToIndex(x+1,z+1);

                indexes[i * 6 + 0] =backLeft;
                indexes[i * 6 + 1] = forwardLeft;
                indexes[i * 6 + 2] = forwardRight;


                indexes[i * 6 + 3] =forwardRight;
                indexes[i * 6 + 4] = backRight;
                indexes[i * 6 + 5] = backLeft;
            }
            
            return new UMeshData(positions,normals,texcoords,new int[][]{indexes});
        }

        public Mesh GenerateMesh(float[,] yMap, Vector2 min, Vector2 max) => GenerateMeshData(yMap, min, max).ToUnity();
    }
}