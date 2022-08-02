using Attributes;
using OpenMetaverse;
using SLUnity.Objects;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace SLUnity
{
    [RequireComponent(typeof(SLPrimitive))]
    public class SLPrimitiveDebug : MonoBehaviour
    {
        private SLPrimitive _slPrimitive;
        private Primitive Self => _slPrimitive.Self;
        private void Awake()
        {
            _slPrimitive = GetComponent<SLPrimitive>();
        }

        private void OnEnable() => UpdateDebug();


        void UpdateDebug()
        {
            UpdateDebugTransform();
            UpdateDebugParent();
        }

        void UpdateDebugTransform()
        {
            Position = Self.Position.CastUnity();
            Rotation = Self.Rotation.CastUnity();
            RotationEuler = Rotation.eulerAngles;
            Scale = Self.Scale.CastUnity();
        }
        [Header("Transform")]
        [ReadOnly] public Vector3 Position;
        [ReadOnly] public Quaternion Rotation;
        [ReadOnly] public Vector3 RotationEuler;
        [ReadOnly] public Vector3 Scale;

        void UpdateDebugParent()
        {
            ParentID = Self.ParentID;
            ParentIsNull = ParentID == 0;
        }
        [Header("Parenting")] [ReadOnly] public uint ParentID;
        [ReadOnly] public bool ParentIsNull;
    }
}
