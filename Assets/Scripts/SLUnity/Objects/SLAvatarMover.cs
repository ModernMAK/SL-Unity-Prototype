using System;
using Attributes;
using OpenMetaverse;
using SLUnity;
using UnityEngine;
using UnityEngine.InputSystem;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class SLAvatarMover : SLAvatarComp
{
    private AgentManager.AgentMovement Mover;


    private const float TurnSpeedMultiplier = 2f;
    
    [SerializeField][ReadOnly]
    private bool movingForward;
    [SerializeField][ReadOnly]
    private bool movingBackward;
    [SerializeField][ReadOnly]
    private bool movingLeft;
    [SerializeField][ReadOnly]
    private bool movingRight;
    
    protected override void OnAwake()
    {
        enabled = false;
        Mover = Manager.Client.Self.Movement;
        Avatar.Initialized += AvatarOnInitialized;
    }

    private void AvatarOnInitialized(object sender, EventArgs e)
    {
        // Debug.Log($"Mover initialized `{gameObject.name}`");
        if (!Avatar.LocalUserAvatar)
        {
            Destroy(this);
            return;
        }

        enabled = true;
        Debug.Log($"Self Mover initialized `{gameObject.name}`");
        var controls = Manager.Controls;
        controls.Forward.started += ForwardOnStarted;
        controls.Forward.canceled += ForwardOnCanceled;
        controls.Backward.started += BackwardOnStarted;
        controls.Backward.canceled += BackwardOnCanceled;
        
        controls.Right.started += RightOnStarted;
        controls.Right.canceled += RightOnCanceled;
        controls.Left.started += LeftOnStarted;
        controls.Left.canceled += LeftOnCanceled;
    }


    private void UpdateZMove(bool newState, bool isForward)
    {
        if (isForward)
            movingForward = Mover.AtPos = newState;
        else
            movingBackward = Mover.AtNeg = newState;
        Mover.SendUpdate(true);
    }
    private void UpdateRotation(bool newState, bool isRight)
    {
        if (isRight)
            movingRight = Mover.TurnRight = newState;
        else
            movingLeft = Mover.TurnLeft = newState;
        Mover.SendUpdate(true);
    }

    private static readonly Vector3 SL_UP = new Vector3(0, 0, 1);
    private void ApplyRotation(float deltaTime)
    {
        //REMEMBER; 
        //  SL Up is Z
        //  Body Rotation is in SL!
        //      Rather than doing conversinos; just work in SL coords
        
        //Either cancels out (Left & Right) or no rotation
        if (Mover.TurnLeft == Mover.TurnRight) return;
        
        var dir = Mover.TurnRight ? -1f : 1f;
        var q = Quaternion.AngleAxis(dir * deltaTime * Mathf.Rad2Deg * TurnSpeedMultiplier, SL_UP);
        Mover.BodyRotation *= q.CastSL();
        Mover.SendUpdate(true);

    }


    private void ForwardOnStarted(InputAction.CallbackContext obj) => UpdateZMove(true, true);
    private void ForwardOnCanceled(InputAction.CallbackContext obj) => UpdateZMove(false, true);
    private void BackwardOnStarted(InputAction.CallbackContext obj) => UpdateZMove(true, false);
    private void BackwardOnCanceled(InputAction.CallbackContext obj) => UpdateZMove(false, false);
    private void RightOnStarted(InputAction.CallbackContext obj) => UpdateRotation(true, true);
    private void RightOnCanceled(InputAction.CallbackContext obj) => UpdateRotation(false, true);
    private void LeftOnStarted(InputAction.CallbackContext obj) => UpdateRotation(true, false);
    private void LeftOnCanceled(InputAction.CallbackContext obj) => UpdateRotation(false, false);

    private void Update()
    {
        ApplyRotation(Time.deltaTime);
        // Vector2 delta = Vector2.zero;
        // // (Input.GetKey(Left));
        //     // delta += Vector2.left;
        // if(Input.GetKey(Right))
        //     delta += Vector2.right;
        // Manager.Client.Self.Movement.AtPos = (Input.GetKey(Forward));
        // Manager.Client.Self.Movement.AtPos = (Input.GetKey(Forward));
        // if(Input.GetKey(Backward))
        //     delta += Vector2.down;
        //
        // if (delta.sqrMagnitude > 0f)
        // {
        //     delta = delta.normalized * Speed;
        //     var newPos = transform.position + new Vector3(delta.x, 0, delta.y);
        //     var slPos = CommonConversion.UnityToCoord(newPos);
        //     Debug.Log($"Moving from {transform.position} to {newPos} ~ ({slPos} on simulator)");
        //     //DO NOT USE SET POS
        //     // Manager.Client.Objects.SetPosition(Manager.Client.Network.CurrentSim,Avatar.Self.LocalID,slPos);
        //     // Manager.Client.Self.Movement.
        // }


    }
}
