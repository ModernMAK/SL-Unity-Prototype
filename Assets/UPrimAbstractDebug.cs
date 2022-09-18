using OpenMetaverse;
using SLUnity.Objects;
using UnityEngine;

public abstract class UPrimAbstractDebug : MonoBehaviour
{
    private UPrimitive _uPrimitive;
    public Primitive Self => _uPrimitive.Self;
    private void Awake()
    {
        _uPrimitive = GetComponent<UPrimitive>();
    }

    private void OnEnable() => UpdateDebug();

    public abstract void UpdateDebug();
}