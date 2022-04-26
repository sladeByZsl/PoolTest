using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.Extention;

    public sealed class AssetBundleLoader : AssetLoaderBase, IEnumerator
    {
        public static AssetBundleManifest AssetBundleManifest { get; set; }

        private static readonly ResourcesLoadMode ms_currentLoadMode = ResourcesLoadMode.AssetBundle_StreamingAssets;

        private AssetBundleTarget[] _dependenceList = null;
        private int _dependencesCount = 0;  // list Length is power of 2, must mark real len

        public AssetBundleTarget mainAssetBundle { get; private set; }
        public bool loadStarted { get; private set; }
        public bool selfDone
        {
            get
            {
                if(mainAssetBundle != null)
                {
                    return mainAssetBundle.isDone;
                }
                return true;
            }
        }
        public bool isDependencesAllDone
        {
            get
            {
                if(_dependenceList != null)
                {
                    for(int i = 0, imax = _dependencesCount; i < imax; i++)
                    {
                        var dpsTarget = _dependenceList[i];
                        if(dpsTarget != null && dpsTarget.isDone == false)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
        public string AssetBundleName { get { return assetName; } }

        public bool releaseMark = false;
        public int unmanagedAssetCount { get; private set; }

        static AssetBundleLoader()
        {
            ms_currentLoadMode = GameConfig.Instance.resourceLoadMode;
        }

        public AssetBundleLoader(string assetBundleName, AssetSource assetSource)
            : base(Utility.AssetBundleNameToAssetBundlePath(assetBundleName, ms_currentLoadMode), assetBundleName, assetSource, false)
        {
            loadStarted = false;
        }

        #region Main Funcs
        // load main asestbundle and dps assetbundles
        public override void LoadRequest(LoadThreadMode loadThreadMode, ThreadPriority priority = ThreadPriority.Normal)
        {
            loadStarted = true;
            bool firstTimeLoad = (mainAssetBundle == null || mainAssetBundle.unloaded);
            // load dps first
            if(firstTimeLoad)
            {
                if(AssetBundleManifest)
                {
                    var dps = AssetBundleManifest.GetAllDependencies(AssetBundleName);
                    if(dps != null && dps.Length > 0)
                    {
                        if(_dependenceList == null)
                        {
                            _dependenceList = ObjectPool.GlobalArrayAllocator<AssetBundleTarget>.Allocate(dps.Length);
                        }
                        _dependencesCount = dps.Length;
                        for(int i = 0, imax = dps.Length; i < imax; i++)
                        {
                            var dpsAssetBundleName = dps[i];
                            var dpsLoadPath = Utility.AssetBundleNameToAssetBundlePath(dpsAssetBundleName, ms_currentLoadMode);
                            var dpsTarget = LoadAssetBundle(dpsLoadPath, dpsAssetBundleName, assetSource, unloadable, false);
                            _dependenceList[i] = dpsTarget;
                            dpsTarget.LoadRequest(loadThreadMode, priority);
                        }
                    }
                }
            }
            else
            {
                if(_dependenceList != null)
                {
                    for(int i = 0, imax = _dependencesCount; i < imax; i++)
                    {
                        var dpsTarget = _dependenceList[i];
                        if(dpsTarget != null && dpsTarget.isDone == false)
                        {
                            dpsTarget.unloadable &= unloadable;
                            dpsTarget.LoadRequest(loadThreadMode, priority);
                        }
                    }
                }
            }
            // load main
            if(firstTimeLoad)
            {
                mainAssetBundle = LoadAssetBundle(loadPath, AssetBundleName, assetSource, unloadable, true);
            }
            mainAssetBundle.unloadable &= unloadable;
            mainAssetBundle.LoadRequest(loadThreadMode, priority);
            this.loadAssetMode = loadThreadMode;
        }
        // increase all ref count
        public void IncreaseReferenceCount(int count)
        {
            if(mainAssetBundle != null)
            {
                // special in scene load, check the count
                if((mainAssetBundle.referenceCount + count) >= 0)
                {
                    mainAssetBundle.IncreaseReferenceCount(count);
                }
            }
            if(_dependenceList != null)
            {
                for(int i = 0, imax = _dependencesCount; i < imax; i++)
                {
                    var dpsTarget = _dependenceList[i];
                    if(dpsTarget != null)
                    {
                        dpsTarget.IncreaseReferenceCount(count);
                    }
                }
            }
        }
        // do unload assetbundle
        public override bool Unload()
        {
            if(false == this.unloaded)
            {
                this.unloaded = true;
                bool unmanagedAsset = false;
                unmanagedAssetCount = 0;
                if(mainAssetBundle != null)
                {
                    unmanagedAsset |= (false == mainAssetBundle.unloadable);
                    if(mainAssetBundle.unloadable)
                    {
                        mainAssetBundle.UnloadRequest(AssetUnloadManager.UnloadLogic.Unload);
                    }
                    else
                    {
                        unmanagedAssetCount++;
                    }
                }
                if(_dependenceList != null)
                {
                    for(int i = 0, imax = _dependencesCount; i < imax; i++)
                    {
                        var dpsTarget = _dependenceList[i];
                        if(dpsTarget != null && dpsTarget.referenceCount <= 0)
                        {
                            unmanagedAsset |= (false == dpsTarget.unloadable);
                            if(dpsTarget.unloadable)
                            {
                                dpsTarget.UnloadRequest(AssetUnloadManager.UnloadLogic.Unload);
                            }
                            else
                            {
                                unmanagedAssetCount++;
                            }
                        }
                    }
                    if(false == releaseMark)
                    {
                        ObjectPool.GlobalArrayAllocator<AssetBundleTarget>.DeAllocate(_dependenceList);
                        _dependenceList = null;
                        _dependencesCount = 0;
                    }
                }
                return unmanagedAsset;
            }
            return false;
        }

        public void Release()
        {
            this.unloaded = true;
            if(mainAssetBundle != null)
            {
                if(false == mainAssetBundle.unloaded)
                {
                    mainAssetBundle.UnloadRequest(AssetUnloadManager.UnloadLogic.Release);
                }
            }
            if(_dependenceList != null)
            {
                for(int i = 0, imax = _dependencesCount; i < imax; i++)
                {
                    var dpsTarget = _dependenceList[i];
                    if(dpsTarget != null && dpsTarget.referenceCount <= 0)
                    {
                        if(false == dpsTarget.unloaded)
                        {
                            dpsTarget.UnloadRequest(AssetUnloadManager.UnloadLogic.Release);
                        }
                    }
                }
                ObjectPool.GlobalArrayAllocator<AssetBundleTarget>.DeAllocate(_dependenceList);
                _dependenceList = null;
                _dependencesCount = 0;
            }
        }

        /// <summary>
        /// force change unload mark
        /// </summary>
        /// <param name="unloadableSet"></param>
        public void ChangeAllUnloadable(bool unloadableSet)
        {
            this.unloadable = unloadableSet;
            if(mainAssetBundle != null)
            {
                mainAssetBundle.unloadable = unloadableSet;
            }
            if(_dependenceList != null)
            {
                foreach(var dependence in _dependenceList)
                {
                    dependence.unloadable = unloadableSet;
                }
            }
        }
        #endregion

        #region Help Funcs
        // load single assetbundle
        private AssetBundleTarget LoadAssetBundle(string loadPath, string assetBundleName, AssetSource assetSource, bool unloadable, bool isMain)
        {
            var target = AssetBundleTarget.AssetBundleTargets.TryGetValue(loadPath);
            if(target == null)
            {
                target = new AssetBundleTarget(loadPath, assetBundleName, assetSource, AssetBundleManifest.GetAssetBundleHash(assetBundleName));
            }
            target.isMain |= isMain;
            target.unloadable &= unloadable;
            return target;
        }
        #endregion

        #region IEnumerator Imp
        public override bool isDone
        {
            get
            {
                if(loadStarted && selfDone)
                {
                    return isDependencesAllDone;
                }
                return false;
            }
        }
        public override bool MoveNext()
        {
            return false == isDone;
        }
        #endregion
    }
}

