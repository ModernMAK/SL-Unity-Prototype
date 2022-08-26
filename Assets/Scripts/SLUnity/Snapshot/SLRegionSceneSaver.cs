using System;
using System.IO;
using System.Linq;
using SLUnity.Managers;
using UnityEngine;

namespace SLUnity.Snapshot
{
    public class SLRegionSceneSaver : MonoBehaviour
    {
        public string Dir;
        public string File;

        public const string Extension = ".scenegraph";
        static void SaveToDisk(string path)
        {
            if (string.IsNullOrEmpty(path))
                path = "CurrentScene" + Extension;
            else if (System.IO.Path.GetExtension(path).ToLower() != Extension)
                path += Extension;
            var primsTracked = SLManager.Instance.Client.Network.CurrentSim.ObjectsPrimitives;
            var terrain = SLManager.Instance.Terrain.GetTerrainHeightmapCopy();
            var primitives = primsTracked.Copy().Values.ToArray();
            var slregion = new SLRegionScene(primitives, terrain);
            var serializer = new SLRegionSerializer();
            try
            {
                using var fstream = new FileStream(path, FileMode.OpenOrCreate);
                using var writer = new BinaryWriter(fstream);
                serializer.Write(writer, slregion);
            }
            catch (IOException ex)
            {
                Debug.LogException(ex);
            }
        }

        [SerializeField] private float _startTime = 100000;
        [SerializeField]
        private bool autosave = false;

        [Min(1)]
        [SerializeField] private float _saveInterval = 1;
        public bool Save = false;
        private void Awake()
        {
            _startTime = Time.time;
        }

        private void Update()
        {
            if (Save)
            {
                Save = false;
                SaveToDisk(Path.Combine(Dir,File));
            }

            if (autosave && Time.time - _startTime >= _saveInterval)
            {
                _startTime = Time.time;
                SaveToDisk(Path.Combine(Dir,File +  $"@ {Time.time - _startTime}"));
            }
        }

    }
}