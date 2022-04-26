using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.Common
{
    using AssetBundleMaster.GameUtilities;
    using AssetBundleMaster.Extention;

    /// <summary>
    /// CoroutineRoot is a timer or schedular that use Coroutine
    /// </summary>
    public class CoroutineRoot : SingletonComponent<CoroutineRoot>
    {
        public const int NULL = -1;
        private static WaitForEndOfFrame ms_waitFrame = new WaitForEndOfFrame();    // cache
        private IDGen m_idGen = new IDGen();

        private Dictionary<int, CoroutineController> m_coroutineController = new Dictionary<int, CoroutineController>();
        private ObjectPool.CommonAllocator<CoroutineController> _coroutineControllerPool;

        private static System.Action<CoroutineController> ms_autoDeAllocate = null;

        #region Override Funcs
        protected override void Initialize()
        {
            _coroutineControllerPool = new ObjectPool.CommonAllocator<CoroutineController>().Set(() =>
            {
                return new CoroutineController();
            }, (_controller) =>
            {
                _controller.Clear();
            });
            ms_autoDeAllocate = new System.Action<CoroutineController>(RemoveCoroutineController);
        }

        protected override void UnInitialize()
        {
        }
        #endregion

        #region Ext Funcs
        /// <summary>
        /// Start With IEnumerator, Base Entrance
        /// </summary>
        /// <param name="enumerator"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        public int StartCoroutineEx(IEnumerator enumerator, System.Action call = null, bool tickNow = false)
        {
            CoroutineController coroutineController = AllocateCoroutineController(enumerator, call);
            if(tickNow)
            {
                if(coroutineController.TickOnce() != CoroutineState.Finished)
                {
                    coroutineController.ReStart();
                    StartCoroutine(coroutineController.Start());
                }
            }
            else
            {
                StartCoroutine(coroutineController.Start());
            }
            return coroutineController.id;
        }
        /// <summary>
        /// Start With yield struction
        /// </summary>
        /// <param name="yield"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        public int StartCoroutineYield(YieldInstruction yield, System.Action<YieldInstruction> call)
        {
            return StartCoroutineEx(RunCoroutineWaitYield(yield, call));
        }
        /// <summary>
        /// Start With AsyncOperation struction
        /// </summary>
        /// <param name="asyncOperation"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        public int StartCoroutineAsyncOperation(AsyncOperation asyncOperation, System.Action<AsyncOperation> call)
        {
            return StartCoroutineEx(RunCoroutineWaitYieldAsyncOperation(asyncOperation, call));
        }
        /// <summary>
        /// Start With wait frames callback
        /// </summary>
        /// <param name="delayFrames"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        public int StartCoroutineWaitFrames(int delayFrames, System.Action call)
        {
            return StartCoroutineEx(RunCoroutineWaitFrames(delayFrames, call));
        }
        /// <summary>
        /// Start With wait time callback
        /// </summary>
        /// <param name="delayTime"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        public int StartCoroutineWaitTime(float delayTime, System.Action call, bool unscaleTime = true)
        {
            return StartCoroutineEx(RunCoroutineWaitTime(delayTime, call, unscaleTime));
        }
        /// <summary>
        /// Start With predicate
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        public int StartCoroutineWaitPredicate(System.Func<bool> predicate, System.Action call)
        {
            return StartCoroutineEx(RunCoroutineWaitPredicate(predicate, call));
        }
        /// <summary>
        /// run call interval
        /// </summary>
        /// <param name="totalTime"></param>
        /// <param name="interval"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        public int StartCoroutineRepeat(float totalTime, float interval, System.Action<float, float, bool> call, bool unscaleTime = true)
        {
            return StartCoroutineEx(RunCoroutineRepeat(totalTime, interval, call, unscaleTime));
        }

#if !UNITY_2017_1_OR_NEWER
        /// <summary>
        /// run call interval
        /// </summary>
        /// <param name="totalTime"></param>
        /// <param name="interval"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        public int StartCoroutineWWW(WWW www, System.Action<WWW> endCall)
        {
            return StartCoroutineEx(RunCoroutineWaitWWW(www, endCall));
        }
#endif

        #endregion

        #region Special Func
        public CoroutineController GetCoroutineController(int id)
        {
            return m_coroutineController.TryGetValue(id);
        }
        public bool IsCoroutineRunning(int id)
        {
            return CoroutineStateEquals(id, CoroutineState.Running);
        }
        public bool IsCoroutinePaused(int id)
        {
            return CoroutineStateEquals(id, CoroutineState.Paused);
        }
        public bool IsCoroutineDone(int id)
        {
            return CoroutineStateEquals(id, CoroutineState.Finished);
        }
        public bool StopCoroutine(ref int id, bool callEnd = false)
        {
            if(id != NULL)
            {
                var controller = m_coroutineController.TryGetValue(id);
                if(controller != null)
                {
                    controller.Stop(callEnd);
                    id = NULL;
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Help Funcs
        public static bool IsNull(int id)
        {
            return NULL == id;
        }

        private CoroutineController AllocateCoroutineController(IEnumerator enumerator, System.Action call)
        {
            CoroutineController coroutineController = _coroutineControllerPool.Allocate();
            coroutineController.id = m_idGen.getNewID;
            coroutineController.Reset(enumerator, call);
            coroutineController.SetDoneCall(ms_autoDeAllocate);
            m_coroutineController.Add(coroutineController.id, coroutineController);
            return coroutineController;
        }

        private bool CoroutineStateEquals(int id, CoroutineState state)
        {
            if(id != NULL)
            {
                var controller = m_coroutineController.TryGetValue(id);
                if(controller != null)
                {
                    return controller.state == state;
                }
            }
            return false;
        }
        private void RemoveCoroutineController(CoroutineController controller)
        {
            if(controller != null)
            {
                m_coroutineController.Remove(controller.id);
                _coroutineControllerPool.DeAllocate(controller);
            }
        }
        private void RemoveCoroutineController(int id)
        {
            if(id != NULL)
            {
                CoroutineController controller = null;
                if(m_coroutineController.TryGetValue(id, out controller))
                {
                    RemoveCoroutineController(controller);
                }
            }
        }
        #endregion

        #region Wrapped Funcs
        private IEnumerator RunCoroutineWaitYield(YieldInstruction yield, System.Action<YieldInstruction> func)
        {
            if(yield != null)
            {
                yield return yield;
            }
            if(func != null)
            {
                func(yield);
            }
        }
        private IEnumerator RunCoroutineWaitYieldAsyncOperation(AsyncOperation asyncOperation, System.Action<AsyncOperation> func)
        {
            if(asyncOperation != null)
            {
                yield return asyncOperation;
            }
            if(func != null)
            {
                func(asyncOperation);
            }
        }
#if !UNITY_2017_1_OR_NEWER
        private IEnumerator RunCoroutineWaitWWW(WWW www, System.Action<WWW> func)
        {
            if(www != null)
            {
                yield return www;
            }
            if(func != null)
            {
                func(www);
            }
        }
#endif
        private IEnumerator RunCoroutineWaitFrames(int frames, System.Action func)
        {
            if(func != null)
            {
                while(frames > 0)
                {
                    frames--;
                    yield return ms_waitFrame;
                }
                func();
            }
        }
        private IEnumerator RunCoroutineWaitTime(float seconds, System.Action func, bool unscaleTime)
        {
            if(func != null)
            {
                if(unscaleTime)
                {
                    yield return new WaitForSecondsRealtime(seconds);
                }
                else
                {
                    yield return new WaitForSeconds(seconds);
                }
                func();
            }
        }
        private IEnumerator RunCoroutineWaitPredicate(System.Func<bool> predicate, System.Action func)
        {
            if(predicate != null)
            {
                while(predicate.Invoke() == false)
                {
                    yield return ms_waitFrame;
                }
            }
            if(func != null)
            {
                func.Invoke();
            }
        }
        private IEnumerator RunCoroutineRepeat(float totalTime, float interval, System.Action<float, float, bool> func, bool unscaleTime)
        {
            if(func != null)
            {
                float total = totalTime;
                float deltaTime = 0.0f;
                float elapsedTime = 0.0f;
                float tempEapsedTime = 0.0f;
                while((total -= deltaTime) > 0.0f)
                {
                    tempEapsedTime += deltaTime;
                    if(tempEapsedTime >= interval)
                    {
                        elapsedTime += tempEapsedTime;
                        tempEapsedTime = 0.0f;
                        func.Invoke(elapsedTime, total, false);
                    }
                    yield return ms_waitFrame;
                    deltaTime = (unscaleTime ? Time.unscaledDeltaTime : Time.deltaTime);
                }
                func.Invoke(total, total, true);
            }
        }
        #endregion
    }
}
