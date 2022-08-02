using System;
using System.Collections.Generic;
using System.Threading;

namespace SLUnity.Threading
{
    public class ThreadRunner
    {
    
        /// <summary>
        /// Actions to run on the main thread.
        /// Can be accessed by any thread.
        /// This does not belong to any thread.
        /// </summary>
        public ThreadQueue<Action> Global { get; private set; }
        /// <summary>
        /// Current actions which have been moved to the main thread.
        /// SHOULD only be accessed on the main thread.
        /// This belongs to the main thread.
        /// </summary>
        private Queue<Action> Local { get; set; }

        public CancellationTokenSource  CancellationTokenSource { get; private set; }
        private Thread _thread;
        public Thread Thread
        {
            get => _thread;
            set
            {
                if (_thread != null)
                    throw new InvalidOperationException("A thread has already ben set for this group!");
                _thread = value;
            } 
        }
    
        public ThreadRunner(Thread thread)
        {
            Thread = thread;
            Local = new Queue<Action>();
            Global = new ThreadQueue<Action>();
            CancellationTokenSource = new CancellationTokenSource ();
        }
    
        public void LocalizeActions()
        {
            var acts = Global.CreateCopy();
            foreach(var _ in acts)
                Local.Enqueue(_);
            Global.Clear();
        }

        public void RunLocals() => RunLocals(Local.Count);
        public void RunLocals(int actions)
        {
            for (var _ = 0; _ < actions && Local.Count > 0; _++)
                (Local.Dequeue())();
        }

        /// <summary>
        /// ONLY USE ON NON-UNITY THREADS
        /// </summary>
        public void MainLoop()
        {
            while (true)
            {
                if(CancellationTokenSource.Token.IsCancellationRequested)
                    break;
                LocalizeActions();
                RunLocals();
                Thread.Sleep(250);
            }
            CancellationTokenSource.Dispose();
        }

        public void AssertThread()
        {
            if (Thread != Thread.CurrentThread)
                throw new Exception("Wrong thread for this action group!");
        }
    

    }
}