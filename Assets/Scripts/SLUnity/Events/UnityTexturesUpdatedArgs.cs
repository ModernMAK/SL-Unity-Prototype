using System;
using UnityEngine;

namespace SLUnity.Events
{
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
}