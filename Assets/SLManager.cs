using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SLManager : MonoBehaviour
{
    public SLClient Client { get; private set; }
    public SLTextureManager TextureManager { get; private set; }
    public SLObjectManager ObjectManager { get; private set; }
    public SLMeshManager MeshManager { get; private set; }

    public static SLManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            throw new InvalidOperationException("Cannot have two instances of an SLClient, it should be a Singleton!");
        Instance = this;
        Client = GetComponent<SLClient>();
        TextureManager = GetComponent<SLTextureManager>();
        ObjectManager = GetComponent<SLObjectManager>();
        MeshManager = GetComponent<SLMeshManager>();
    }
}
