using System;
using System.Collections.Generic;
using System.IO;
using OpenMetaverse;
using SLUnity.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SLUnity.Managers
{
    public class AssetBundleDiskCache<TKey,TValue> where TValue : Object
    {
        private readonly string _cacheDir;
        private readonly Func<TKey, string> _cachePath;
        private readonly Func<TKey, string> _assetPath;
        private readonly Dictionary<TKey, TValue> _localCache;

        public AssetBundleDiskCache(string cacheDir, Func<TKey, string> cachePath, Func<TKey, string> assetPath)
        {
            _cacheDir = cacheDir;
            _cachePath = cachePath;
            _assetPath = assetPath;
            _localCache = new Dictionary<TKey, TValue>();
        }

        private string GetFilePath(TKey key) => Path.Combine(_cacheDir, _cachePath(key));
        
        public bool Load(TKey key, out TValue value)
        {
            //Unity whines if we reload an asset bundle; since it has the same assets loaded
            //  But you can't reference them without an asset bundle, which means somebody needs to cache bundles
            //      Or at the very least; their assets
            if (_localCache.TryGetValue(key, out value))
                return true;
            var fPath = GetFilePath(key);
            var aPath = _assetPath(key);
            try
            {
                using var fstream = File.Open(fPath, FileMode.Open);
                var bundle = AssetBundle.LoadFromStream(fstream);
                value = bundle.LoadAsset<TValue>(aPath);
                _localCache[key] = value;
                return true;
            }
            catch (FileNotFoundException exception)
            {
                value = default;
                return false;
            }
        }
    }
}