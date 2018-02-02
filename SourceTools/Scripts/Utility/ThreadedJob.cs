using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadedJob {
    private bool m_IsDone = false;
    private object m_Handle = new object();
    private System.Threading.Thread m_Thread = null;
    public bool IsDone
    {
        get
        {
            return m_IsDone;
        }
        set
        {
            lock (m_Handle)
            {
                m_IsDone = value;
            }
        }
    }

    public virtual void Start()
    {
        m_Thread = new System.Threading.Thread(Run);
        m_Thread.IsBackground = true;
        m_Thread.Priority = System.Threading.ThreadPriority.BelowNormal;
        m_Thread.Start ();
    }
    public virtual void Abort()
    {
        m_Thread.Abort();
    }

    protected virtual void ThreadFunction() { }

    protected virtual void OnFinished() { }

    public virtual bool Update()
    {
        if (IsDone)
        {
            OnFinished();
            return true;
        }
        return false;
    }
    public IEnumerator WaitFor()
    {
        while(!Update())
        {
            yield return null;
        }
    }
    private void Run()
    {
        ThreadFunction();
        IsDone = true;
    }
}