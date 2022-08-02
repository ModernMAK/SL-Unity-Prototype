using System;
using OpenMetaverse;
using UnityEngine;

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