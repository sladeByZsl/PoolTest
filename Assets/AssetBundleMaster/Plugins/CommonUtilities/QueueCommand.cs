using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.Common
{
    public class Command : IEnumerator
    {
        public object data;
        public System.Action<Command> cmd = null;
        public System.Func<Command, bool> finishFunc = null;

        public virtual object Current
        {
            get
            {
                return null;
            }
        }

        public virtual bool MoveNext()
        {
            return finishFunc != null && finishFunc(this) == false;
        }

        public virtual void Reset()
        {
        }
    }

    public class QueueCommands : Command
    {
        private Queue<Command> m_queueCmds = new Queue<Command>();
        private int _controller = CoroutineRoot.NULL;

        private System.Action _endCall = null;
        public System.Action excute
        {
            get
            {
                if(_endCall == null)
                {
                    _endCall = new System.Action(Excute);
                }
                return _endCall;
            }
        }

        public bool isRunning
        {
            get
            {
                return CoroutineRoot.Instance.IsCoroutineRunning(_controller);
            }
        }

        #region Main Funcs
        public void Enqueue(Command cmd, bool excuteNow = true)
        {
            m_queueCmds.Enqueue(cmd);
            if(excuteNow)
            {
                Excute();
            }
        }
        public void Excute()
        {
            if(false == isRunning)
            {
                var cmd = PopFirst();
                if(cmd != null)
                {
                    if(cmd.cmd != null)
                    {
                        cmd.cmd.Invoke(cmd);
                    }
                    if(cmd.MoveNext())
                    {
                        _controller = CoroutineRoot.Instance.StartCoroutineEx(cmd, excute, true);
                    }
                    else
                    {
                        Excute();
                    }
                }
            }
        }
        public void Clear(System.Action<Queue<Command>> access = null)
        {
            CoroutineRoot.Instance.StopCoroutine(ref _controller);
            if(access != null)
            {
                access.Invoke(m_queueCmds);
            }
            m_queueCmds.Clear();
        }
        #endregion

        #region Overrides
        public override bool MoveNext()
        {
            return m_queueCmds.Count > 0 || isRunning;
        }

        #endregion

        #region Help Fnucs
        private Command PopFirst()
        {
            if(m_queueCmds != null && m_queueCmds.Count > 0)
            {
                return m_queueCmds.Dequeue();
            }
            return null;
        }
        #endregion
    }
}
