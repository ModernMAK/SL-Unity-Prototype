using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SLTerrain : MonoBehaviour
{
    private MeshFilter _meshFilter;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
    }

    public void SetMesh(Mesh mesh)
    {
        _meshFilter.mesh = mesh;
    }
}
