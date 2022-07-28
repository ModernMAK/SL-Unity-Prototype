using System;
using OpenMetaverse;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class SLTransform : MonoBehaviour
{
    
    [SerializeField]
    private Transform _mesh;
    [SerializeField]
    private Transform _container;

    public void SetChild(SLTransform child) => child.SetParent(this);



    public void SetParent(SLTransform parent)
    {
        var container = parent._container;
        transform.parent = container;
    }
    public void SetParent(Transform parent)
    {
        transform.parent = parent;
    }

    public Vector3 LocalPosition
    {
        get => transform.localPosition;
        set => transform.localPosition = value;
    }
    public Vector3 WorldPosition
    {
        get => transform.position;
        set => transform.position = value;
    }
    public Quaternion LocalRotation
    {
        get => transform.localRotation;
        set => transform.localRotation = value;
    }

    public Quaternion WorldRotation
    {
        get => transform.rotation;
        set => transform.rotation = value;
    }

    public Vector3 Scale
    {
        get => _mesh.localScale;
        set =>  _mesh.localScale = value;
    }

}
