using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;

public class ActionGroup
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
    
    public ActionGroup(Thread thread)
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

[RequireComponent(typeof(SLObjectManager))]
public class SLThreadManager : SLBehaviour
{
    public ActionGroup Unity { get; private set; }
    public ActionGroup Data { get; private set; }

    private void Awake()
    {
        Unity = new ActionGroup(Thread.CurrentThread);
        //Unfortunately, creating a thread requires an entry point
        //  BUT, our entrypoint is inside our class, that we want to reference our thread in
        Data = new ActionGroup(null);
        Data.Thread = new Thread(Data.MainLoop);
        Data.Thread.Start();
    }

    private void OnApplicationQuit()
    {
        if(Data != null)
            Data.CancellationTokenSource.Cancel();
    }

    private void Update()
    {
        
        Unity.LocalizeActions();
        Unity.RunLocals();
    }
}