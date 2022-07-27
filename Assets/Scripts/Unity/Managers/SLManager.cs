using System;
using System.Collections;
using System.Collections.Generic;
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

    private void Awake()
    {
        Assertions.AssertSingleton(this,Instance,nameof(SLManager));
        Instance = this;
        Client = GetComponent<SLClient>();
        TextureManager = GetComponent<SLTextureManager>();
        PrimitiveManager = GetComponent<SLPrimitiveManager>();
        MeshManager = GetComponent<SLMeshManager>();
        Threading = GetComponent<SLThreadManager>();
    }
}
