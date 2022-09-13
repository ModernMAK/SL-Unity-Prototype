using System;
using OpenMetaverse;
using OpenMetaverse.Rendering;
using SLUnity.Data;
using SLUnity.Objects;
using SLUnity.Rendering;
using SLUnity.Threading;
using UnityEngine;
using Mesh = UnityEngine.Mesh;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace SLUnity.Managers
{
    public class SLTerrainManager : SLBehaviour
    {
        private readonly IHeightMapMeshGenerator _terrainGen = new TerrainMeshGenerator();
        
        private const int MAP_MAX = 255;
        private ThreadVar<float[,]> _terrainMap;
        private ThreadVar<bool> _updating;
        private ThreadVar<bool> _dirty;
        private ThreadVar<Mesh> _terrainMesh;
    
        private SLTerrain _terrain;
        [SerializeField] private GameObject _prefab;

        public float[,] GetTerrainHeightmapCopy()
        {
            var copy = new float[MAP_MAX + 1, MAP_MAX + 1];
            lock (_terrainMap.SyncRoot)
            {
                var src = _terrainMap.Unsynchronized;
                for(var x =0; x < MAP_MAX+1;x++)
                for (var y = 0; y < MAP_MAX + 1; y++)
                    copy[x, y] = src[x, y]; 
            }
            return copy;
        }
        public void SetTerrainHeightmap(float[,] heightMap)
        {
            if (heightMap.GetLength(0) != _terrainMap.Synchronized.GetLength(0) ||
                heightMap.GetLength(1) != _terrainMap.Synchronized.GetLength(1))
                throw new Exception("Invalid Height Map Size!");
            lock (_terrainMap.SyncRoot)
            {
                var dest = _terrainMap.Unsynchronized;
                var src = heightMap;
                for(var x =0; x < MAP_MAX+1;x++)
                for (var y = 0; y < MAP_MAX + 1; y++)
                    dest[x, y] = src[x, y]; 
            }
            lock (_updating.SyncRoot)
            {
                if (!_updating.Unsynchronized)
                {
                    _updating.Unsynchronized = true;
                    Manager.Threading.Data.Global.Enqueue(UpdateMesh);
                }
                else
                {
                    lock (_dirty.SyncRoot)
                    {
                        _dirty.Unsynchronized = true;
                    }
                }
            }
        }
        
        private void Awake()
        {
            _terrainMesh = new ThreadVar<Mesh>(new Mesh());
            _terrainMesh.Unsynchronized.name = "Terrain";
            _terrainMap = new ThreadVar<float[,]>(new float[MAP_MAX+1, MAP_MAX+1]);
            _updating = new ThreadVar<bool>();
            _dirty = new ThreadVar<bool>(true);
            if (_terrain == null)
            {
                var inst = Instantiate(_prefab);
                inst.name = "Terrain";
                inst.isStatic = true;
                inst.transform.parent = transform;
                _terrain = inst.GetComponent<SLTerrain>();
            }
            _terrain.SetMesh(_terrainMesh.Unsynchronized);
        }

        private void OnEnable()
        {
            Manager.Client.Terrain.LandPatchReceived += TerrainOnLandPatchReceived;
        }

        private void OnDisable()
        {
            Manager.Client.Terrain.LandPatchReceived -= TerrainOnLandPatchReceived;
        }

        private void TerrainOnLandPatchReceived(object sender, LandPatchReceivedEventArgs e)
        {
            lock (_terrainMap.SyncRoot)
            {
                var map = _terrainMap.Unsynchronized;
                var xO = e.X * e.PatchSize;
                var zO = e.Y * e.PatchSize;
                for (var x = 0; x < e.PatchSize; x++)
                for (var z = 0; z < e.PatchSize; z++)
                {
                    var i = (z * e.PatchSize) + x;
                    map[xO + x,zO + z] = e.HeightMap[i];
                }
            }


            lock (_updating.SyncRoot)
            {
                if (!_updating.Unsynchronized)
                {
                    _updating.Unsynchronized = true;
                    Manager.Threading.Data.Global.Enqueue(UpdateMesh);
                }
                else
                {
                    lock (_dirty.SyncRoot)
                    {
                        _dirty.Unsynchronized = true;
                    }
                }
            }
        }


        [Obsolete("Use _terrainGen.GenreateMesh()")]
        private UMeshData GenerateMesh(float[,] yMap, Vector2 min, Vector2 max)
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
                var north = new int2(0, 1);
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
    
        private void UpdateMesh()
        {
            const float MIN = 0f;
            const float MAX = 255f;
            lock (_terrainMap.SyncRoot)
            {
                var map = _terrainMap.Unsynchronized;
                var meshData = _terrainGen.GenerateMeshData(
                    map,
                    Vector2.one * MIN, 
                    Vector2.one * MAX
                );
                // var face = MeshGen.TerrainMesh(
                //     map,
                //     MIN, 
                //     MAX,
                //     MIN,
                //     MAX
                // );
                // var mesh = AssembleUMeshData(face);
                Manager.Threading.Unity.Global.Enqueue(() => GenerateMesh(meshData));
            }
        }

        private UMeshData AssembleUMeshData(Face slFace)
        {
        
            var vertList = new Vector3[slFace.Vertices.Count];
            var normList = new Vector3[slFace.Vertices.Count];
            var uvList = new Vector2[slFace.Vertices.Count];
            var indList = new int[slFace.Indices.Count];
            var counter = 0;
            for(var i = 0; i < slFace.Vertices.Count; i++)
            {
                var v = slFace.Vertices[i];
                vertList[i] = (CommonConversion.CoordToUnity(v.Position));
                normList[i] = (CommonConversion.CoordToUnity(v.Normal));
                uvList[i] = (CommonConversion.UVToUnity(v.TexCoord));
            }


            for (var i = 0; i < indList.Length; i++)
                indList[i] = slFace.Indices[slFace.Indices.Count - (i + 1)];
            // indList.AddRange(slFace.Indices.Select(i => (int)i));
            var result = new UMeshData(
                vertList,
                normList,
                uvList,
                new int[][]{indList}
            );
            return result;
        }

        private void GenerateMesh(UMeshData umesh)
        {
            lock (_terrainMesh.SyncRoot)
            {
                umesh.ToUnity(_terrainMesh.Unsynchronized);
            }

            _updating.Synchronized = false;
            lock (_dirty.SyncRoot)
            {
                if (_dirty.Unsynchronized)
                {
                    _dirty.Unsynchronized = false;
                    Manager.Threading.Data.Global.Enqueue(UpdateMesh);
                } 
            
            }
        }


    
    
    }
    //Hack to avoid importing mathmatics

    internal struct int2
    {
        public int2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public readonly int x;
        public readonly int y;

        public static int2 operator +(int2 left, int2 right) => new int2(left.x + right.x, left.y + right.y);
        public static int2 operator -(int2 left, int2 right) => new int2(left.x - right.x, left.y - right.y);

    }
}
