using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.Common;

    public enum AssetSource
    {
        Resources,
        AssetDataBase,
        LocalAssetBundle,
        RemoteAssetBundle,
    }
    public enum LoadState
    {
        Ready,
        Loading,
        Loaded,
        Error
    }

    public abstract class AssetLoaderBase : IEnumerator
    {
        public string loadPath { get; protected set; }
        public string assetName { get; protected set; }

        public LoadThreadMode loadAssetMode { get; protected set; }
        public AssetSource assetSource { get; protected set; }

        public virtual bool isDone { get; protected set; }
        public bool unloaded { get; protected set; }

        public UnityEngine.Object[] Assets { get; protected set; }
        public readonly WrapCall<AssetLoaderBase> completed;

        public bool unloadable = true;
        public bool haveAssets { get { return Assets != null && Assets.Length > 0; } }

        public AssetLoaderBase(string loadPath, string assetName, AssetSource assetSource, bool useCallBack)
        {
            this.loadPath = loadPath;
            this.assetName = assetName;
            this.assetSource = assetSource;
            if(useCallBack)
            {
                this.completed = new WrapCall<AssetLoaderBase>(this);
            }
        }

        #region Main Funcs
        public abstract void LoadRequest(LoadThreadMode loadThreadMode, ThreadPriority priority = ThreadPriority.Normal);
        public abstract bool Unload();
        #endregion

        #region IEnumerator IMP
        public virtual object Current { get { return null; } }

        public abstract bool MoveNext();

        public virtual void Reset() { }

        public virtual void Call()
        {
            if(completed != null)
            {
                if(isDone)
                {
                    completed.Call();
                }
                else
                {
                    completed.WrappedCall();
                }
            }
        }
        #endregion
    }

    public sealed class WrapCall<T> where T : IEnumerator
    {
        public T self { get; private set; }

        private event System.Action<T> call;
        private int _loadedCallEnumerator = CoroutineRoot.NULL;

        public WrapCall(T self)
        {
            this.self = self;
        }

        public void PushCall(System.Action<T> act)
        {
            if(act != null)
            {
                this.call += act;
            }
        }

        public void SetCall(System.Action<T> act)
        {
            this.call = act;
        }

        public void RemoveCall(System.Action<T> act)
        {
            if(act != null)
            {
                this.call -= act;
            }
        }

        public void WrappedCall()
        {
            if(_loadedCallEnumerator == CoroutineRoot.NULL)
            {
                _loadedCallEnumerator = CoroutineRoot.Instance.StartCoroutineEx(self, Call, true);
            }
            else if(CoroutineRoot.Instance.IsCoroutineDone(_loadedCallEnumerator))
            {
                Call();
            }
        }

        public void Call()
        {
            if(call != null)
            {
                var tempCall = call;
                call = null;
                tempCall.Invoke(self);
            }
        }

        public void Clear()
        {
            CoroutineRoot.Instance.StopCoroutine(ref _loadedCallEnumerator);
            call = null;
        }
    }

}

