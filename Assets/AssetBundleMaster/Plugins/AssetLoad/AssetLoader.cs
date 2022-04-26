using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.Common;
    using AssetBundleMaster.ContainerUtilities;
    using AssetBundleMaster.Extention;

    public class AssetLoader : AssetLoaderBase, IEnumerator
    {
        public AssetBundleLoader assetBundleLoader { get; protected set; }
        protected AssetBundleRequest _assetBundleRequest = null;
        protected ResourceRequest _resourceRequest = null;
        protected int _assetBundleLoadController = CoroutineRoot.NULL;
        protected int _assetLoadController = CoroutineRoot.NULL;

        public System.Type systemTypeInstance { get; private set; }
        public LoadState loadState { get; protected set; }
        public override bool isDone
        {
            get { return loadState == LoadState.Loaded || loadState == LoadState.Error; }
        }
        private bool _loadAllAssets = false;
        public bool loadAllAssets
        {
            get
            {
                return _loadAllAssets;
            }
            set
            {
                if(_loadAllAssets != value)
                {
                    if(value)
                    {
                        loadState = LoadState.Ready;    // reset load
                    }
                    _loadAllAssets |= value;
                }
            }
        }

        public class UnloadedAssetLoader
        {
            public string loadPath { get; set; }
            public AssetBundleLoader assetBundleLoader { get; set; }

            public void Release()
            {
                if(assetBundleLoader != null)
                {
                    assetBundleLoader.Release();
                    assetBundleLoader = null;
                }
            }
        }

        public static readonly Dictionary<UnloadedAssetLoader, WeakReference<UnityEngine.Object>[]> UnloadedAssets = new Dictionary<UnloadedAssetLoader, WeakReference<UnityEngine.Object>[]>();
        public static readonly List<UnloadedAssetLoader> RemovedAssets = new List<UnloadedAssetLoader>();

        const bool UnloadCacheSizePowerOf2 = false;

        public AssetLoader(string loadPath, string assetName, System.Type systemTypeInstance, AssetSource assetSource, AssetBundleLoader assetBundleLoader, bool useCallBack)
            : base(loadPath, assetName, assetSource, useCallBack)
        {
            this.systemTypeInstance = systemTypeInstance;
            this.assetBundleLoader = assetBundleLoader;
            loadState = LoadState.Ready;
        }

        #region Main Funcs
        public override void LoadRequest(LoadThreadMode loadThreadMode, ThreadPriority priority = ThreadPriority.Normal)
        {
            if(loadState == LoadState.Loading && loadAssetMode == loadThreadMode)
            {
                return;     // dont need to do any thing
            }
            // if the load mode changed from Async -> Sync, will force reload asset bundle, it will throw an error though
            bool reload = (loadState == LoadState.Loading && loadAssetMode != loadThreadMode);
            loadAssetMode = loadThreadMode;
            // if mode changed, loading is still reloadable
            if(false == isDone)
            {
                loadState = LoadState.Loading;
                switch(assetSource)
                {
#if UNITY_EDITOR
                    // editor mode, load from assetdatabase, no async mode
                    case AssetSource.AssetDataBase:
                        {
                            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath(loadPath, systemTypeInstance);
                            OnAssetLoaded(asset);
                        }
                        break;
#endif
                    // Load Resources directly, we must check is it a file with ext
                    case AssetSource.Resources:
                        {
                            string resLoadPath = loadPath;
                            if(System.IO.Path.HasExtension(loadPath))
                            {
                                var dirName = System.IO.Path.GetDirectoryName(loadPath);
                                var fileName = System.IO.Path.GetFileNameWithoutExtension(loadPath);
                                resLoadPath = string.IsNullOrEmpty(dirName) ? fileName : string.Concat(dirName, "/", fileName);
                            }
                            switch(loadThreadMode)
                            {
                                case LoadThreadMode.Synchronous:
                                    {
                                        if(loadAllAssets)
                                        {
                                            var tags = Resources.LoadAll(resLoadPath, this.systemTypeInstance);
                                            OnAssetLoaded(tags);
                                        }
                                        else
                                        {
                                            var tag = Resources.Load(resLoadPath, this.systemTypeInstance);
                                            OnAssetLoaded(tag);
                                        }
                                    }
                                    break;
                                case LoadThreadMode.Asynchronous:
                                    {
                                        if(loadAllAssets)
                                        {
                                            var tags = Resources.LoadAll(resLoadPath, this.systemTypeInstance);  // Resources dont have load all async API
                                            OnAssetLoaded(tags);
                                            CoroutineRoot.Instance.StopCoroutine(ref _assetBundleLoadController);
                                        }
                                        else
                                        {
                                            if(_resourceRequest == null)
                                            {
                                                _resourceRequest = Resources.LoadAsync(resLoadPath, systemTypeInstance);
                                                _assetBundleLoadController = CoroutineRoot.Instance.StartCoroutineYield(_resourceRequest, OnResourceRequest);
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    // All AssetBundle Load From file are the same here
                    default:
                        {
                            if(assetBundleLoader != null)
                            {
                                if(reload)
                                {
                                    assetBundleLoader.unloadable &= this.unloadable;
                                    assetBundleLoader.LoadRequest(loadThreadMode, priority);
                                }
                                if(assetBundleLoader.isDone)
                                {
                                    OnAssetBundleLoaded();
                                }
                                else
                                {
                                    if(CoroutineRoot.IsNull(_assetBundleLoadController))
                                    {
                                        _assetBundleLoadController = CoroutineRoot.Instance.StartCoroutineEx(assetBundleLoader, OnAssetBundleLoaded);
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            loadAssetMode = loadThreadMode;
            LoadedCheck(false);
        }

        public override bool Unload()
        {
            bool hasAsset = Assets != null && Assets.Length > 0;
            if(this.unloaded == false)
            {
                this.unloaded = true;   // if unloaded, reference of AssetLoader will be cleared
                UnloadAssetBundle();
                Assets = null;
                if(completed != null)
                {
                    completed.Clear();
                }
            }
            return hasAsset;
        }
        #endregion

        #region Help Funcs
        protected void UnloadAssetBundle()
        {
            if(assetBundleLoader != null)
            {
                CoroutineRoot.Instance.StopCoroutine(ref _assetBundleLoadController);
                assetBundleLoader.IncreaseReferenceCount(-1);    // decrease ref count

                // if main asset has loaded asset, listen to the weak reference
                if(assetBundleLoader.mainAssetBundle != null && (false == assetBundleLoader.mainAssetBundle.unloadable))
                {
                    if(this.haveAssets)
                    {
                        var unloadedAssetLoader = new UnloadedAssetLoader();
                        unloadedAssetLoader.loadPath = this.loadPath;
                        unloadedAssetLoader.assetBundleLoader = this.assetBundleLoader;
                        unloadedAssetLoader.assetBundleLoader.releaseMark = true;
                        var cacheArray = WeakReference<UnityEngine.Object>.AllocateArray(Assets.Length, UnloadCacheSizePowerOf2);
                        for(int i = 0; i < Assets.Length; i++)
                        {
                            cacheArray[i] = WeakReference<UnityEngine.Object>.Allocate(Assets[i]);
                        }
                        UnloadedAssets[unloadedAssetLoader] = cacheArray;
                    }
                    else if(assetBundleLoader.mainAssetBundle.referenceCount == 0)
                    {
                        assetBundleLoader.Release();
                    }
                }
                assetBundleLoader = null;
            }
        }
        private void LoadedCheck(bool checkState = true)
        {
            if(checkState)
            {
                loadState = Assets != null ? LoadState.Loaded : LoadState.Error;
            }
            Call();
        }
        public void ChangeAllUnloadable(bool unloadableSet)
        {
            this.unloadable = unloadableSet;
            if(assetBundleLoader != null)
            {
                assetBundleLoader.ChangeAllUnloadable(unloadableSet);
            }
        }
        #endregion

        #region Enevts
        protected virtual void OnAssetBundleLoaded()
        {
            if(assetBundleLoader != null && assetBundleLoader.mainAssetBundle != null && assetBundleLoader.mainAssetBundle.assetBundle)
            {
                var assetBundle = assetBundleLoader.mainAssetBundle.assetBundle;
                switch(loadAssetMode)
                {
                    case LoadThreadMode.Synchronous:
                        {
                            if(this.loadAllAssets)
                            {
                                var tags = string.IsNullOrEmpty(assetName)
                                    ? assetBundle.LoadAllAssets(systemTypeInstance)
                                    : assetBundle.LoadAssetWithSubAssets(assetName, systemTypeInstance);
                                OnAssetLoaded(tags);
                            }
                            else
                            {
                                if(string.IsNullOrEmpty(assetName))
                                {
                                    var tags = assetBundle.LoadAllAssets(systemTypeInstance);
                                    OnAssetLoaded(tags);
                                }
                                else
                                {
                                    var tag = assetBundle.LoadAsset(assetName, systemTypeInstance);
                                    OnAssetLoaded(tag);
                                }
                            }
                        }
                        break;
                    case LoadThreadMode.Asynchronous:
                        {
                            if(this.loadAllAssets)
                            {
                                CoroutineRoot.Instance.StopCoroutine(ref _assetLoadController);
                                _assetBundleRequest = string.IsNullOrEmpty(assetName)
                                    ? assetBundle.LoadAllAssetsAsync(systemTypeInstance)
                                    : assetBundle.LoadAssetWithSubAssetsAsync(assetName, systemTypeInstance);
                                _assetLoadController = CoroutineRoot.Instance.StartCoroutineYield(_assetBundleRequest, OnAssetBundleRequest);
                            }
                            else
                            {
                                if(_assetBundleRequest == null)
                                {
                                    _assetBundleRequest = string.IsNullOrEmpty(assetName)
                                        ? assetBundle.LoadAllAssetsAsync(systemTypeInstance)
                                        : assetBundle.LoadAssetAsync(assetName, systemTypeInstance);
                                    _assetLoadController = CoroutineRoot.Instance.StartCoroutineYield(_assetBundleRequest, OnAssetBundleRequest);
                                }
                            }
                        }
                        break;
                }
            }
            else
            {
                OnAssetLoaded(new UnityEngine.Object[0]);
            }
        }
        protected void OnAssetBundleRequest(YieldInstruction asyncOperation)
        {
            if(false == isDone)
            {
                UnityEngine.Object[] assets = null;
                if(_assetBundleRequest.isDone)
                {
                    assets = _assetBundleRequest.allAssets;
                }
                OnAssetLoaded(assets);
            }
        }
        protected void OnResourceRequest(YieldInstruction asyncOperation)
        {
            if(false == isDone)
            {
                if(_resourceRequest.isDone)
                {
                    OnAssetLoaded(_resourceRequest.asset);
                }
            }
        }

        // on asset loaded wrap
        protected void OnAssetLoaded(UnityEngine.Object asset)
        {
            if(false == isDone && asset)
            {
                Assets = Utility.CreateAssetArray(this.systemTypeInstance, 1);
                Assets[0] = asset;
            }
            LoadedCheck();
        }
        protected void OnAssetLoaded(UnityEngine.Object[] assets)
        {
            if(false == isDone && assets != null && assets.Length > 0)
            {
                Assets = Utility.CreateAssetArray(this.systemTypeInstance, assets.Length);
                if(assets.Length > 0)
                {
                    System.Array.Copy(assets, Assets, assets.Length);
                }
            }
            LoadedCheck();
        }

        public static void CheckUnload()
        {
            if(UnloadedAssets.Count > 0)
            {
                foreach(var kv in UnloadedAssets)
                {
                    var loader = kv.Key;
                    if(loader.assetBundleLoader.mainAssetBundle.referenceCount <= 0)
                    {
                        bool hasAsset = false;
                        var array = kv.Value;
                        for(int i = 0; i < array.Length; i++)
                        {
                            var weak = array[i];
                            if(weak.Target != null)
                            {
                                hasAsset = true;
                                break;
                            }
                        }
                        if(false == hasAsset)
                        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            Debug.Log("Asset Unreferenced ===> " + loader.loadPath);
#endif
                            loader.Release();
                            RemovedAssets.Add(loader);
                        }
                    }
                }
                if(RemovedAssets.Count > 0)
                {
                    foreach(var key in RemovedAssets)
                    {
                        WeakReference<UnityEngine.Object>[] list = null;
                        if(UnloadedAssets.TryGetValue(key, out list))
                        {
                            UnloadedAssets.Remove(key);
                            WeakReference<UnityEngine.Object>.DeAllocateArray(ref list, UnloadCacheSizePowerOf2);
                        }
                    }
                    RemovedAssets.Clear();
                }
            }
        }
        #endregion

        #region IEnumerator Imp
        public override bool MoveNext()
        {
            return false == isDone;
        }
        #endregion

    }
}

