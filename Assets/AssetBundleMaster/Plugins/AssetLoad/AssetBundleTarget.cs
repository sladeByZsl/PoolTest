using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.Common;

    /// <summary>
    /// Load Single AssetBundle
    /// </summary>
    public sealed class AssetBundleTarget : AssetLoaderBase, IEnumerator
    {
        public static readonly Dictionary<string, AssetBundleTarget> AssetBundleTargets = new Dictionary<string, AssetBundleTarget>();

        public AssetBundle assetBundle { get; private set; }
        public AsyncOperation assetBundleRequest { get; private set; }

        public int referenceCount { get; private set; }

        public LoadState loadState { get; private set; }    // this is a mark in case assetbundle not exists, force to make it done for further step
        public string AssetBundleName { get { return assetName; } }
        public Hash128 assetBundleHash { get; private set; }

        public bool isMain = false;

        /// <summary>
        /// notice if
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="assetBundleName"></param>
        /// <param name="assetSource"></param>
        /// <param name="dependencies"></param>
        /// <param name="priority"></param>
        /// <param name="version"></param>
        public AssetBundleTarget(string loadPath, string assetBundleName, AssetSource assetSource, Hash128 assetBundleHash)
            : base(loadPath, assetBundleName, assetSource, false)
        {
            this.loadPath = loadPath;
            this.assetSource = assetSource;
            loadState = LoadState.Ready;
            this.assetBundleHash = assetBundleHash;
            AssetBundleTargets[loadPath] = this;
        }

        #region Main Funcs
        // request load
        public override void LoadRequest(LoadThreadMode loadThreadMode, ThreadPriority priority = ThreadPriority.Normal)
        {
            this.unloaded = false;
            if(loadState == LoadState.Loaded)
            {
                return;
            }
            if(loadState == LoadState.Loading)
            {
                if(loadAssetMode == loadThreadMode)
                {
                    return;     // dont need to do any thing
                }
                else
                {
                    loadState = LoadState.Ready;
                    LoadModeChanged(loadThreadMode);    // dps all have to change load mode
                    return;
                }
            }

            loadAssetMode = loadThreadMode;
            loadState = LoadState.Loading;
            switch(assetSource)
            {
                // load from local file
                case AssetSource.LocalAssetBundle:
                    {
                        switch(loadThreadMode)
                        {
                            case LoadThreadMode.Synchronous:
                                {
                                    if(assetBundle == false)
                                    {
                                        assetBundle = AssetBundle.LoadFromFile(loadPath);
                                    }
                                    OnLoaded();
                                }
                                break;
                            case LoadThreadMode.Asynchronous:
                                {
                                    if(assetBundle == false)
                                    {
                                        assetBundleRequest = AssetBundle.LoadFromFileAsync(loadPath);
                                        assetBundleRequest.priority = (int)priority;
#if UNITY_2017_1_OR_NEWER
                                        assetBundleRequest.completed += OnLocalAssetBundleLoaded;
#else
                                        CoroutineRoot.Instance.StartCoroutineAsyncOperation(assetBundleRequest, OnLocalAssetBundleLoaded);
#endif
                                    }
                                    else
                                    {
                                        OnLoaded();
                                    }
                                }
                                break;
                        }
                    }
                    break;
                // local from remote or cache
                case AssetSource.RemoteAssetBundle:
                    {
                        assetBundleRequest = AssetBundleWebRequestController.Instance.DownloadAssetBundle(loadPath, assetBundleHash, OnRemoteAssetBundleLoaded);
                        if(loadThreadMode == LoadThreadMode.Synchronous && loadState != LoadState.Loaded)
                        {
                            Debug.LogError("Remote Mode do not support load Sync");
                        }
                    }
                    break;
            }
        }
        // Mark Load Mode Changed
        public void LoadModeChanged(LoadThreadMode loadBundleMode)
        {
            if(loadAssetMode != loadBundleMode)
            {
                this.LoadRequest(loadBundleMode);
            }
        }
        // increase ref count
        public void IncreaseReferenceCount(int count)
        {
            referenceCount += count;
        }
        // do unload assetbundle
        public override bool Unload()
        {
            if(false == this.unloaded)
            {
                this.unloaded = unloadable;
                if(unloadable)
                {
                    Release(true);
                }
            }
            return this.unloaded;
        }
        // force release
        public void Release(bool forceRelease)
        {
            if(assetSource == AssetSource.RemoteAssetBundle)
            {
                AssetBundleWebRequestController.Instance.Unload(loadPath, forceRelease);
            }
            else
            {
                if(assetBundle)
                {
                    assetBundle.Unload(forceRelease);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log(string.Concat("AssetBundle.Unload(", forceRelease, ") : ", assetName, " At Frame : ", Time.frameCount));
#endif
                }
            }
            ResetData();
            this.unloaded = true;
        }

        public void UnloadRequest(AssetUnloadManager.UnloadLogic logic)
        {
            AssetUnloadManager.Instance.EnqueueUnloadTarget(this, logic);
        }
        // reset state for reuse
        public void ResetData()
        {
            assetBundle = null;
            assetBundleRequest = null;  
            loadState = LoadState.Ready;
            unloadable = true;
        }
        #endregion

        #region Delegate
        void OnLocalAssetBundleLoaded(AsyncOperation asyncOperation)
        {
            if(asyncOperation.isDone)
            {
                if(assetBundle == false)
                {
                    assetBundle = (asyncOperation as AssetBundleCreateRequest).assetBundle;
                }
            }
            OnLoaded();
        }
        void OnRemoteAssetBundleLoaded(AssetBundle assetBundle)
        {
            this.assetBundle = assetBundle;
            OnLoaded();
        }
        // self loaded
        private void OnLoaded()
        {
            loadState = assetBundle ? LoadState.Loaded : LoadState.Error;
        }
        #endregion

        #region Help Funcs
        #endregion

        #region IEnumerator Imp
        public override bool isDone
        {
            get { return loadState == LoadState.Loaded || loadState == LoadState.Error; }
        }
        public override bool MoveNext()
        {
            return false == isDone;
        }
        #endregion
    }
}

