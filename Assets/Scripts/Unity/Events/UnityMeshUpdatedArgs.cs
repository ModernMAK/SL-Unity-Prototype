using System;
using UnityEngine;

public class UnityMeshUpdatedArgs : EventArgs
{
    public UnityMeshUpdatedArgs(Mesh newMesh)
    {
        NewMesh = newMesh;
    }

    public Mesh NewMesh { get; private set; }
}