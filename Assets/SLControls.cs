using System;
using Attributes;
using UnityEngine;
using UnityEngine.InputSystem;

public class SLControls : MonoBehaviour
{
    [SerializeField]
    private InputActionAsset _mapping;

    [SerializeField] [ReadOnly] private InputAction _LeftAction;
    [SerializeField] [ReadOnly] private InputAction _RightAction;
    [SerializeField] [ReadOnly] private InputAction _ForwardAction;
    [SerializeField] [ReadOnly] private InputAction _BackwardAction;
    
    private void Awake()
    {
        _mapping.Enable();
        _LeftAction = _mapping.FindAction("Left", true);
        _RightAction = _mapping.FindAction("Right", true);
        _ForwardAction = _mapping.FindAction("Forward", true);
        _BackwardAction = _mapping.FindAction("Backward", true);
    }

    public InputAction Left => _LeftAction;
    public InputAction Right => _RightAction;
    public InputAction Forward => _ForwardAction;
    public InputAction Backward => _BackwardAction;
    
    
}
