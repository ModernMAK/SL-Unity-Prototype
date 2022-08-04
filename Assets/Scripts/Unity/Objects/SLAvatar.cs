using System;
using Attributes;
using OpenMetaverse;
using UnityEditor;
using UnityEngine;
using Avatar = OpenMetaverse.Avatar;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Unity.Objects
{
    public class SLAvatar : SLBehaviour
    {
        // private static readonly IRendering MeshGen = new MeshmerizerR();
        //
        // [SerializeField] [ReadOnly] private int _requestedTextures; //
        //
        // [SerializeField][ReadOnly]
        // private Mesh _mesh;
        // [SerializeField] [ReadOnly] private Texture[] _textures;
        // [SerializeField] [ReadOnly] private Texture _defaultTexture;
        [SerializeField] [ReadOnly] private bool _awoken = false;
        [SerializeField] [ReadOnly] private PrimType _pType;
        [SerializeField] [ReadOnly] private string _sculptTex;
        [SerializeField] [ReadOnly] private bool _isThisUser;
        public bool LocalUserAvatar { get => _isThisUser; private set => _isThisUser = value; }
        public Avatar Self { get; private set; }
        [SerializeField][ReadOnly] private Vector3 _targetPosition;
        [SerializeField][ReadOnly] private Quaternion _targetRotation;
        [SerializeField][ReadOnly] private Vector3 _targetScale;
        public event EventHandler Initialized;
        //
        protected virtual void Awake()
        {
            // _requestedTextures = 0;
            // _textures = null;
            // _defaultTexture = null;
            _awoken = true;
            Initialized += DoDebug;
        }

        private void DoDebug(object sender, EventArgs e)
        {
            _sculptTex = (Self.Sculpt != null ? Self.Sculpt.SculptTexture.Guid.ToString() : Guid.Empty.ToString());
            _pType = Self.Type;
        }
        public void Initialize(Avatar self)
        {
            if (!_awoken)
                throw new Exception("Initialize occured before gameobject initialization finished!");
            if (Self != null)
                throw new ArgumentException("Primitive Object has already been initialized!", nameof(self));
            Self = self;
            LocalUserAvatar = (self.Name == Manager.Client.Self.Name); //HACK
            StartLerp(transform.position,transform.rotation, transform.localScale);
            OnInitialized();
        }
        protected virtual void OnInitialized()
        {
            Initialized?.Invoke(this, EventArgs.Empty);
        }

        public void StartLerp(Vector3 avatarPosition, Quaternion avatarRotation, Vector3 avatarScale)
        {
            // _targetPosition = avatarPosition;
            // _targetRotation = avatarRotation;
            // _targetScale = avatarScale;
            transform.position = avatarPosition;
            transform.rotation = avatarRotation;
            transform.localScale = avatarScale;
        }
        //
        // private Vector3 Lerp(Vector3 src, Vector3 dest)
        // {
        //     const float ERR = 0.01f;
        //     const float ERR_SQR = ERR * ERR;
        //     const float TIME = 0.5f;
        //
        //     return (src - dest).sqrMagnitude > ERR_SQR ? Vector3.Lerp(src, dest, TIME) : dest;
        // }
        // private Quaternion Lerp(Quaternion src, Quaternion dest)
        // {
        //     const float ERR = 0.01f;
        //     const float ERR_SQR = ERR * ERR;
        //     const float TIME = 0.5f;
        //
        //     return (src.eulerAngles - dest.eulerAngles).sqrMagnitude > ERR_SQR ? Quaternion.Lerp(src, dest, TIME) : dest;
        // }
        // private void Update()
        // {
        //     transform.position = Lerp(transform.position, _targetPosition);
        //     transform.rotation = Lerp(transform.rotation, _targetRotation);
        //     transform.localScale = Lerp(transform.localScale, _targetScale);
        //
        // }
    }
}