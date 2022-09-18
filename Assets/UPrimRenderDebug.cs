using System;
using Attributes;
using OggVorbisEncoder.Setup;
using OpenMetaverse;
using SLUnity;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class UPrimRenderDebug : UPrimAbstractDebug
{
    [Serializable]
    public class USculptData
    {
        public USculptData(Primitive.SculptData sculptData)
        {
            Invert = sculptData.Invert;
            Mirror = sculptData.Mirror;
            Type = sculptData.Type;
            SculptTexture = sculptData.SculptTexture.Guid.ToString();
        }
        
        public bool Invert;
        public bool Mirror;
        public SculptType Type;
        public string SculptTexture;
    }
    [Serializable]
    public class UTextureEntry
    {
        [Serializable]
        public class UTextureEntryFace
        {
            public UTextureEntryFace(Primitive.TextureEntryFace textureEntryFace)
            {
                Bump = textureEntryFace.Bump;
                Fullbright = textureEntryFace.Fullbright;
                Glow =  textureEntryFace.Glow;
                Rotation = textureEntryFace.Rotation;
                Shiny = textureEntryFace.Shiny;
                MediaFlags = textureEntryFace.MediaFlags;
                Offset = new Vector2( textureEntryFace.OffsetU, textureEntryFace.OffsetV);
                Repeat = new Vector2(textureEntryFace.RepeatU, textureEntryFace.RepeatV);
                MaterialID = textureEntryFace.MaterialID.Guid.ToString();
                TexMapType = textureEntryFace.TexMapType;
                TextureID = textureEntryFace.TextureID.Guid.ToString();
                RGBA = textureEntryFace.RGBA.CastUnity();
            }

            public Bumpiness Bump;
            public bool Fullbright;
            public float Glow;
            public float Rotation;
            public Shininess Shiny;
            public bool MediaFlags;
            public Vector2 Offset;
            public Vector2 Repeat;
            public string MaterialID;
            public string TextureID;
            public MappingType TexMapType;
            public Color RGBA;
        }

        public UTextureEntry(Primitive.TextureEntry textureEntry)
        {

            DefaultTextures = new UTextureEntryFace(textureEntry.DefaultTexture);
            FaceTextures = new UTextureEntryFace[textureEntry.FaceTextures.Length];
            for (var i = 0; i < textureEntry.FaceTextures.Length; i++)
                if(textureEntry.FaceTextures[i] != null)
                    FaceTextures[i] = new UTextureEntryFace(textureEntry.FaceTextures[i]);
        }
        
        public UTextureEntryFace DefaultTextures;
        public UTextureEntryFace[] FaceTextures;
    }

    [SerializeField] [ReadOnly]
    private USculptData Sculpt;
    
    [SerializeField] [ReadOnly]
    private UTextureEntry Textures;

    
    
    public override void UpdateDebug()
    {
        Sculpt = new USculptData(Self.Sculpt);
        Textures = new UTextureEntry(Self.Textures);

    }
}
