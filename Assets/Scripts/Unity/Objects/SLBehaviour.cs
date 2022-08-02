using System;
using Unity.Managers;
using UnityEngine;

public class SLBehaviour : MonoBehaviour
{
    [Obsolete("Use Manager.Client instead")]
    protected SLClient Client => SLClient.Instance;
    protected SLManager Manager => SLManager.Instance;
    
}