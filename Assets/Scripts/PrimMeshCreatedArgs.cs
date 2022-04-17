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
public class TextureCreatedArgs : EventArgs
{
    public TextureCreatedArgs(UUID id, Texture texture)
    {
        Texture = texture;
        Id = id;
    }

    public UUID Id { get; private set; }
    public Texture Texture { get; private set; }
}
public class UnityMeshUpdatedArgs : EventArgs
{
    public UnityMeshUpdatedArgs(Mesh newMesh)
    {
        NewMesh = newMesh;
    }

    public Mesh NewMesh { get; private set; }
}
public class UnityTexturesUpdatedArgs : EventArgs
{
    public UnityTexturesUpdatedArgs(int textureIndex, Texture newTexture)
    {
        TextureIndex = textureIndex;
        NewTexture = newTexture;
    }

    public int TextureIndex { get; private set; }
    public Texture NewTexture { get; private set; }
}