using Unity.Objects;
using UnityEngine;

[RequireComponent(typeof(SLAvatar))]
public class SLAvatarComp : SLBehaviour
{
    private void Awake()
    {
        _avatar = GetComponent<SLAvatar>();
        OnAwake();
        
    }

    protected virtual void OnAwake(){}

    private SLAvatar _avatar;
    public SLAvatar Avatar => _avatar;

}