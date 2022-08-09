using System;
using System.IO;
using OpenMetaverse;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SLUnity.Managers
{
    public class AssetBundleDiskCache<TKey,TValue> where TValue : Object
    {
        private readonly string _cacheDir;
        private readonly Func<TKey, string> _cachePath;
        private readonly Func<TKey, string> _assetPath;

        public AssetBundleDiskCache(string cacheDir, Func<TKey, string> cachePath, Func<TKey, string> assetPath)
        {
            _cacheDir = cacheDir;
            _cachePath = cachePath;
            _assetPath = assetPath;
        }

        private string GetFilePath(TKey key) => Path.Combine(_cacheDir, _cachePath(key));
        
        public bool Load(TKey key, out TValue value)
        {
            try
            {
                var fPath = GetFilePath(key);
                var aPath = _assetPath(key);
                using var fstream = File.Open(fPath, FileMode.Open);
                var bundle = AssetBundle.LoadFromStream(fstream);
                value = bundle.LoadAsset<TValue>(aPath);
                return true;
            }
            catch (IOException exception)
            {
                value = default;
                return false;
            }
        }
    }
}