using System;
using SLUnity.Managers;
using UnityEngine;

namespace SLUnity.Objects
{
    public class SLBehaviour : MonoBehaviour
    {
        [Obsolete("Use Manager.Client instead")]
        protected SLClient Client => SLClient.Instance;
        protected SLManager Manager => SLManager.Instance;
    
    }
}