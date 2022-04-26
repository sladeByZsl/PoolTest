using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AssetBundleMaster.Common
{
    public enum CoroutineState
    {
        Ready,
        Running,
        Paused,
        Finished
    }

    public class CoroutineController
    {
        public int id = -1;
        public bool callEndWithBrake = false;
        public bool onceCall = true;
        private IEnumerator _enumerator = null;
        private System.Action _callBack = null;
        private System.Action<CoroutineController> _doneCall = null;
        public CoroutineState state { get; private set; }
        public bool isDone { get { return this.state == CoroutineState.Finished; } private set { this.state = CoroutineState.Finished; } }
        public bool isRunning { get { return this.state == CoroutineState.Running; } private set { this.state = CoroutineState.Running; } }
        public bool isPaused { get { return this.state == CoroutineState.Paused; } private set { this.state = CoroutineState.Paused; } }

        public CoroutineController() { Clear(); }

        public CoroutineController(IEnumerator enumerator, System.Action callBack)
        {
            Reset(enumerator, callBack);
        }

        public void Reset(IEnumerator enumerator, System.Action callBack)
        {
            _enumerator = enumerator;
            state = CoroutineState.Ready;
            _callBack = callBack;
            callEndWithBrake = false;
            onceCall = true;
            _doneCall = null;
        }

        public void SetDoneCall(System.Action<CoroutineController> doneCall)
        {
            _doneCall = doneCall;
        }

        public void Clear()
        {
            Reset(null, null);
        }

        private void Call(bool runCallBack)
        {
            if(runCallBack)
            {
                if(_callBack != null)
                {
                    var tempCall = _callBack;
                    if(onceCall)
                    {
                        _callBack = null;
                    }
                    tempCall.Invoke();
                }
            }
            if(_doneCall != null)
            {
                var tempCall = _doneCall;
                _doneCall = null;
                tempCall.Invoke(this);
            }
        }

        public IEnumerator Start()
        {
            if(state != CoroutineState.Ready || _enumerator == null)
            {
                yield break;
            }

            state = CoroutineState.Running;
            while(_enumerator != null && _enumerator.MoveNext())
            {
                yield return _enumerator.Current;
                while(state == CoroutineState.Paused)
                {
                    yield return null;
                }
                if(state == CoroutineState.Finished)
                {
                    Call(callEndWithBrake);
                    yield break;
                }
            }

            state = CoroutineState.Finished;
            Call(true);
        }

        public void Stop(bool callEnd = true)
        {
            callEndWithBrake = callEnd;
            if(state != CoroutineState.Running && state != CoroutineState.Paused)
            {
                return;
            }
            state = CoroutineState.Finished;
        }

        public void Pause()
        {
            if(state != CoroutineState.Running)
            {
                return;
            }
            state = CoroutineState.Paused;
        }

        public void Resume()
        {
            if(state != CoroutineState.Paused)
            {
                return;
            }
            state = CoroutineState.Running;
        }

        public void ReStart()
        {
            state = CoroutineState.Ready;
        }

        #region Special Usage
        public CoroutineState TickOnce()
        {
            if(isDone)
            {
                Call(true);
                return state;
            }
            state = CoroutineState.Running;
            if(_enumerator == null || _enumerator.MoveNext() == false)
            {
                isDone = true;
            }
            if(isDone)
            {
                Call(true);
            }
            return state;
        }
        #endregion

    }
}