using System;
using OpenMetaverse;
using UnityEngine;

public class PrimMeshCreatedArgs : EventArgs
{
    public PrimMeshCreatedArgs(Primitive primitive, Mesh generatedMesh)
    {
        GeneratedMesh = generatedMesh;
        Owner = primitive;
    }

    public Primitive Owner { get; private set; }
    public Mesh GeneratedMesh { get; private set; }
}