using System;
using System.Collections;
using System.Collections.Generic;
using OpenMetaverse;
using UnityEngine;

namespace Unity.Managers
{
    public class MeshCache : Threadable, IDictionary<Primitive,Mesh>
    {
        private readonly Dictionary<Primitive.ConstructionData, Mesh> _generated;
        private readonly Dictionary<UUID, Mesh> _assets; //TODO 

        public MeshCache()
        {
            _assets = new Dictionary<UUID, Mesh>();
            _generated = new Dictionary<Primitive.ConstructionData, Mesh>();
        }

        public IEnumerator<KeyValuePair<Primitive, Mesh>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<Primitive, Mesh> item) => Set(item.Key, item.Value);

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<Primitive, Mesh> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<Primitive, Mesh>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<Primitive, Mesh> item) => throw new NotImplementedException();

        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return _assets.Count + _generated.Count;
                }
            }
        } 

        public bool IsReadOnly => throw new NotImplementedException();

        private Mesh Get(Primitive primitive)
        {
            return TryGetValue(primitive, out var mesh) ? mesh : null;
        }
        private bool TryGet(Primitive primitive, out Mesh mesh)
        {
            lock(SyncRoot)
            {
                switch (primitive.Type)
                {
                    case PrimType.Mesh:
                        //DOWNLOAD
                        var assetKey = primitive.Sculpt.SculptTexture;
                        return _assets.TryGetValue(assetKey, out mesh);
                        break;
                    case PrimType.Sculpt:
                        // throw new NotSupportedException("Sculpted mesh is currently not supported");
                        mesh = null;
                        return false;
                    case PrimType.Unknown:
                        throw new ArgumentException();
                    case PrimType.Box:
                    case PrimType.Cylinder:
                    case PrimType.Prism:
                    case PrimType.Sphere:
                    case PrimType.Torus:
                    case PrimType.Tube:
                    case PrimType.Ring:
                        //GENERATE
                        var genKey = primitive.PrimData;
                        return _generated.TryGetValue(genKey, out mesh);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        private void Set(Primitive primitive, Mesh mesh)
        {
            lock(SyncRoot)
            {
                switch (primitive.Type)
                {
                    case PrimType.Mesh:
                        //DOWNLOAD
                        var assetKey = primitive.Sculpt.SculptTexture;
                        _assets[assetKey] = mesh;
                        break;
                    case PrimType.Sculpt:
                        throw new NotSupportedException("Sculpted mesh is currently not supported");
                    case PrimType.Unknown:
                        throw new InvalidOperationException();
                    case PrimType.Box:
                    case PrimType.Cylinder:
                    case PrimType.Prism:
                    case PrimType.Sphere:
                    case PrimType.Torus:
                    case PrimType.Tube:
                    case PrimType.Ring:
                        //GENERATE
                        var genKey = primitive.PrimData;
                        _generated[genKey] = mesh;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void Add(Primitive key, Mesh value) => Set(key, value);

        public bool ContainsKey(Primitive key)=> TryGet(key, out _);

        public bool Remove(Primitive key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(Primitive key, out Mesh value) => TryGet(key, out value);

        public Mesh this[Primitive key]
        {
            get => TryGet(key, out var mesh) ? mesh : throw new KeyNotFoundException();
            set => Set(key, value);
        }

        public ICollection<Primitive> Keys =>  throw new NotImplementedException();

        public ICollection<Mesh> Values => throw new NotImplementedException();
    }
}