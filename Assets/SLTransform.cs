using System;
using OpenMetaverse;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class SLTransform : MonoBehaviour
{
    
    private Transform _transform;
    [SerializeField]
    private Transform _mesh;
    [SerializeField]
    private Transform _container;

    public void SetChild(SLTransform child) => child.SetParent(this);



    public void SetParent(SLTransform parent)
    {
        var changed = (parent != Parent);
        if(Parent != null)
            Parent.TransformChanged -= MyParentTransformChanged;
        _transform.parent = _container; // Group in heiarchy; does not do anything
        _parent = parent;
        if(Parent != null)
            Parent.TransformChanged += MyParentTransformChanged;
        if(changed)
            OnParentChanged();
    
    }

    public void UpdateFromPrim(Primitive prim)
    {
        var worldScale = CommonConversion.CoordToUnity(prim.Scale); 
        var localPosition = CommonConversion.CoordToUnity(prim.Position);
        var localRotation = CommonConversion.RotToUnity(prim.Rotation);
        var localScale = worldScale - ParentScale;

        var changed = (_localPos != localPosition) || (_localRotation != localRotation) || (_localScale != localScale);
        _localScale = localScale;
        _localPos = localPosition;
        _localRotation = localRotation;
        if (changed)
            OnTransformChanged();
    }
    
    private void MyParentTransformChanged(object sender, EventArgs e) => TransformChanged?.Invoke(sender, e);


    private void Awake()
    {
        _transform = transform;
    }

    private void OnEnable()
    {
        TransformChanged += UpdateMeshTransform;
    }
    private void OnDisable()
    {
        TransformChanged -= UpdateMeshTransform;
    }

    private void UpdateMeshTransform(object sender, EventArgs e)
    {
        //Locals to avoid matrix calculations;
        //  Should all be Identity matrix values
        _mesh.localPosition = WorldPosition;
        _mesh.localRotation = WorldRotation;
        _mesh.localScale = WorldScale;
    }
    
    private SLTransform _parent;

    public SLTransform Parent
    {
        get => _parent;
        set
        {
            var changed = (_parent != null);
            _parent = value;
            if (changed)
                OnParentChanged();
        }
    }

    private Vector3 _localPos;
    public Vector3 LocalPosition
    {
        get => _localPos;
        set
        {
            var changed = (_localPos != value);
            _localPos = value;
            if(changed)
                OnTransformChanged();
        }
    }
    public Vector3 ParentPosition => Parent != null ? Parent.WorldPosition : Vector3.zero;

    public Vector3 WorldPosition
    {
        get => LocalPosition + ParentPosition;
        set => LocalPosition = value - ParentPosition;
    }
    private Quaternion _localRotation;
    public Quaternion LocalRotation
    {
        get => _localRotation;
        set
        {
            var changed = (_localRotation != value);
            _localRotation = value;
            if(changed)
                OnTransformChanged();
        }
    }

    public Quaternion ParentRotation => Parent != null ? Parent.WorldRotation : Quaternion.identity;
    public Quaternion WorldRotation
    {
        get => LocalRotation * ParentRotation;
        set => LocalRotation = value * Quaternion.Inverse(ParentRotation);
    }

    private Vector3 _localScale;
    public Vector3 LocalScale
    {
        get => _localScale;
        set
        {
            var changed = (_localScale != value);
            _localScale = value;
            if(changed)
                OnTransformChanged();
        }
    }

    public Vector3 ParentScale => Parent != null ? Parent.WorldScale : Vector3.zero;
    public Vector3 WorldScale
    {
        get => LocalScale + ParentScale;
        set => LocalScale = value - ParentScale;
    }

    public event EventHandler TransformChanged;
    public event EventHandler ParentChanged;

    protected virtual void OnTransformChanged()
    {
        TransformChanged?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnParentChanged()
    {
        ParentChanged?.Invoke(this, EventArgs.Empty);
    }
}
