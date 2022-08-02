using System;
using OpenMetaverse;
using SLUnity.Events;
using UnityEngine;
using Material = UnityEngine.Material;
using Vector2 = UnityEngine.Vector2;

// [RequireComponent(typeof(SLPrimitive))]
namespace SLUnity.Objects
{
    [RequireComponent(typeof(MeshFilter))]
    public class SLPrimitiveRenderer : SLBehaviour
    {
        [SerializeField]
        private SLPrimitive _primitive;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private static readonly int BaseMap = Shader.PropertyToID("_AlbedoTex");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Offset = Shader.PropertyToID("_Offset");
        private static readonly int Repeat = Shader.PropertyToID("_Repeat");
        private static readonly int Rotation = Shader.PropertyToID("_Rotation");
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        // private static Shader SimpleShader;
    
        private static readonly string SimpleShaderName = "Shader Graphs/SimpleSL";
        private static Shader _simpleShader;
    
        private static readonly string AlphaShaderName = "Shader Graphs/AlphaSL";
        private static Shader _alphaShader;

        private static Shader GetShader(string name, ref Shader shader)
        {
        
            if (shader != null) return shader;
            
            shader = Shader.Find(name);
            
            if (shader != null) return shader;
            
            throw new NullReferenceException(name);
        }

        public static Shader SimpleShader => GetShader(SimpleShaderName, ref _simpleShader);
        public static Shader AlphaShader => GetShader(AlphaShaderName, ref _alphaShader);
        private static object _renderLock;
        private void Awake()
        {
            _renderLock = new object();
            lock (_renderLock)
            {
                if (_primitive == null)
                    _primitive = GetComponent<SLPrimitive>();
        
                _meshFilter = GetComponent<MeshFilter>();
                _meshRenderer = GetComponent<MeshRenderer>();
                _primitive.UnityMeshUpdated += PrimitiveOnUnityMeshUpdated;
                _primitive.UnityTexturesUpdated += PrimitiveOnUnityTexturesUpdated;
                for(var i = 0; i < _meshRenderer.materials.Length; i++)
                    _meshRenderer.materials[i] = new Material(SimpleShader);
            
            }
            // _primitive.Initialized += PrimitiveOnInitialized;
            // if (SimpleShader == null)
            //     SimpleShader = ;
            // if (SimpleShader == null)
            //     throw new NullReferenceException();
            // else
            // {
            //     Debug.Log(SimpleShader);
            //     Debug.Break();
            // }
        }


        // private void PrimitiveOnInitialized(object sender, EventArgs e)
        // {
        //     // return;
        //     
        //     // var mat = _meshRenderer.material;
        //     lock (_renderLock)
        //     {
        //         // if (SimpleShader == null)
        //         //     throw new NullReferenceException();
        //         //THIS IS A COPY?! GD IT UNITY! UPDATE YOUR FING DOCSTRINGS!
        //         var mats = new Material[_primitive.Self.Textures.FaceTextures.Length];
        //         for (var i = 0; i < mats.Length; i++)
        //         {
        //             mats[i] = new Material(Shader);
        //         }
        //
        //         _meshRenderer.materials = mats;
        //     }
        // }

        private void SetTexture(int index, Texture texture, Shader shader, int texID, bool nullOnly=false)
        {
            var mats = _meshRenderer.materials;
            if (index >= mats.Length)
            {
                mats = new Material[index + 1];
                for (var i = 0; i < _meshRenderer.materials.Length;i++)
                    mats[i] = _meshRenderer.materials[i];
            
                for (var i = _meshRenderer.materials.Length; i < mats.Length; i++)
                    mats[i] = new Material(shader);
            } 
            // throw new IndexOutOfRangeException($"{index} / {mats.Length}");
            var mat = mats[index];
            if (mat.shader != shader)
                mat = mats[index] = new Material(shader);
            if (!nullOnly || mat.GetTexture(texID) == null)
                mat.SetTexture(texID, texture);
        }
    
        private void PrimitiveOnUnityTexturesUpdated(object sender, UnityTexturesUpdatedArgs e)
        {
            const int DEFAULT_TEX = -1;
            lock (_renderLock)
            {
                var matCount = _meshRenderer.materials.Length;
                //Update all textures if default loaded
                if(e.TextureIndex == DEFAULT_TEX)
                    for (var i = 0; i < matCount; i++) 
                        UpdateTexture(i);
                else if (e.TextureIndex < matCount)
                    UpdateTexture(e.TextureIndex);
                else
                    throw new IndexOutOfRangeException($"{e.TextureIndex} >= materials[{matCount}]");
            }

        }

        private void UpdateTexture(int i)
        {
            var materials = _meshRenderer.materials;
            var uPrim = _primitive;
            var prim = uPrim.Self;
        
            var useDefault = prim.Textures.FaceTextures[i] == null;
        
            var textureUsed = useDefault ? uPrim.UnityDefaultTexture.Synchronized :  uPrim.UnityTextures[i];
            var textureInfo = useDefault ? prim.Textures.DefaultTexture : prim.Textures.FaceTextures[i];
            if(textureUsed == null)
                return;

            var color = textureInfo.RGBA.CastUnity();

            var hasAlpha = ((Texture2D)textureUsed).alphaIsTransparency || color.a < 0.999f;
            var shader = hasAlpha ? AlphaShader : SimpleShader;
            var mat = materials[i];
            bool reassign = false;
            if (mat == null || mat.shader != shader)
            {
                materials[i] = mat = new Material(shader);
                reassign = true; //WE HAVE TO REASSIGN _meshRenderer.materials, because `materials` is a COPY!
            }
        

            mat.SetTexture(BaseMap, textureUsed);
            mat.SetColor(BaseColor,color);
            mat.SetVector(Offset, new Vector2(textureInfo.OffsetU, textureInfo.OffsetV));
            mat.SetVector(Repeat,new Vector2(textureInfo.RepeatU,textureInfo.RepeatV));
            mat.SetFloat(Rotation,textureInfo.Rotation);
            mat.SetFloat(Smoothness,Shiny2Smooth(textureInfo.Shiny));
            if(reassign)
                _meshRenderer.materials = materials;

        }

        private float Shiny2Smooth(Shininess shiny)
        {
            return shiny switch
            {
                Shininess.High => 1,
                Shininess.None => 0f,
                Shininess.Low => 1f / 3f,
                Shininess.Medium => 2f / 3f,
                _ => throw new ArgumentOutOfRangeException(nameof(shiny), shiny, null)
            };
        }

        private void PrimitiveOnUnityMeshUpdated(object sender, UnityMeshUpdatedArgs e)
        {
            lock (_renderLock)
            {
                _meshFilter.mesh = e.NewMesh;
                _meshRenderer.materials = new Material[e.NewMesh.subMeshCount];
                for (var i = 0; i < _meshRenderer.materials.Length;i++) 
                    UpdateTexture(i);
            }

            // Debug.Log("DEBUG MESH: Updated!");
        }
    }
}
