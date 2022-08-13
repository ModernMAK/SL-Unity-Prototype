using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
[ExecuteInEditMode]
public class Scene2AssetBundle : MonoBehaviour
{
    public string AssetPath;

    public bool Run;

    public string OutputDir;
#if UNITY_EDITOR
    void Update()
    {
        if (Run)
        {
            Run = false;
            var bundle = new AssetBundleBuild()
            {
                assetBundleName = "RegionTest",
                assetNames = new string[] { AssetPath }
            };
            BuildPipeline.BuildAssetBundles(OutputDir, new AssetBundleBuild[] { bundle }, BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows64);
            
        }
        
    }
    #endif
}
