using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public static class Assertions
{
    public static void AssertSingleton(object self, object singletonReference, string name = null)
    {
        if (singletonReference == null || singletonReference == self) return;
        name ??= "Singleton";//no name given, just set to singleton
        //TODO change exception to more specific exception
        throw new Exception($"Multiple instances of `{name}` found!");
    }
}
public class SLManager : MonoBehaviour
{
    public SLClient Client { get; private set; }
    public SLTextureManager TextureManager { get; private set; }
    public SLPrimitiveManager PrimitiveManager { get; private set; }
    public SLMeshManager MeshManager { get; private set; }

    public static SLManager Instance { get; private set; }

    public SLThreadManager Threading { get; private set; }

    [Obsolete("Use Thread Manager")]
    public Thread UnityThread { get; private set; }

    [Obsolete]
    public void AssertNotUnityThread()
    {
        if (UnityThread == null)
            throw new Exception("Unity Thread was not initialized!!!");
        var check = UnityThread != Thread.CurrentThread;
        if (!check)
            throw new Exception("Currently on Unity Thread!");
    }
    [Obsolete]

    public void AssertUnityThread()
    {
        if (UnityThread == null)
            throw new Exception("Unity Thread was not initialized!!!");
        var check = UnityThread != Thread.CurrentThread;
        if (check)
            throw new Exception("Not currently on Unity Thread!");
    }
    private void Awake()
    {
        UnityThread = Thread.CurrentThread;//Garunteed to be main thread
        
        Assertions.AssertSingleton(this,Instance,nameof(SLManager));
        Instance = this;
        Client = GetComponent<SLClient>();
        TextureManager = GetComponent<SLTextureManager>();
        PrimitiveManager = GetComponent<SLPrimitiveManager>();
        MeshManager = GetComponent<SLMeshManager>();
        Threading = GetComponent<SLThreadManager>();
    }
}
