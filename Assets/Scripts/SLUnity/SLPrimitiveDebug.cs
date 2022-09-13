using Attributes;
using OpenMetaverse;
using SLUnity.Objects;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace SLUnity
{
    [RequireComponent(typeof(UPrimitive))]
    public class SLPrimitiveDebug : MonoBehaviour
    {
        private UPrimitive _uPrimitive;
        private Primitive Self => _uPrimitive.Self;
        private void Awake()
        {
            _uPrimitive = GetComponent<UPrimitive>();
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
