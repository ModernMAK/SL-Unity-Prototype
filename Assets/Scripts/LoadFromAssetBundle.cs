using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadFromAssetBundle : MonoBehaviour
{
    public string AssetBundlePath;

    public bool Load;
    // Update is called once per frame
    void Update()
    {
        if (Load)
        {
            Load = false;
            var bundle = AssetBundle.LoadFromFile(AssetBundlePath);
            var scenes = bundle.GetAllScenePaths();
            SceneManager.LoadScene(scenes[0], LoadSceneMode.Additive);
        }
        
    }
}
