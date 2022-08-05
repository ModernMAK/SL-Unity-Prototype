using System.Collections.Generic;
using System.Threading;
using SLUnity.Objects;
using SLUnity.Threading;
using UnityEngine;

namespace SLUnity.Managers
{
    [RequireComponent(typeof(SLPrimitiveManager))]
    public class SLThreadManager : SLBehaviour
    {
        public ThreadRunner Unity { get; private set; }
        public ThreadRunner Data { get; private set; }
        public ThreadRunner IO { get; private set; }

        [Min(-1)]
        public int UnityActions = -1;
        private IEnumerable<ThreadRunner> Runners
        {
            get
            {
                yield return Data;
                yield return IO;
            }
        }

        private static ThreadRunner CreateThreadRunner()
        {
            var tr = new ThreadRunner(null);
            tr.Thread = new Thread(tr.MainLoop);
            tr.Thread.Start();
            return tr;
        }
    
        private void Awake()
        {
            Unity = new ThreadRunner(Thread.CurrentThread);
            //Unfortunately, creating a thread requires an entry point
            //  BUT, our entrypoint is inside our class, that we want to reference our thread in
            Data = CreateThreadRunner();
            IO = CreateThreadRunner();
        }

        private void OnApplicationQuit()
        {
            foreach(var tr in Runners)
            {
                tr?.CancellationTokenSource.Cancel();
            }
        }

        private void Update()
        {
        
            Unity.LocalizeActions();
            if (UnityActions >= 0)
                Unity.RunLocals(UnityActions);
            else
                Unity.RunLocals(UnityActions);
        }
    }
}