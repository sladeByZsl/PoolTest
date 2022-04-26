using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AssetBundleMaster.ResourceLoad
{
    using AssetBundleMaster.AssetLoad;
    using AssetBundleMaster.Extention;
    using AssetBundleMaster.ContainerUtilities;
    using AssetBundleMaster.Common;
    using TypeDataPool = ObjectPool.GlobalAllocator<ResourcePool.TypeData>;
    using TypeDataListPool = ObjectPool.GlobalAllocator<List<ResourcePool.TypeData>>;

    public static class ResourcePool
    {
        #region Defines
        public class TypeData
        {
            public Type type { get; private set; }

            public WeakReference<UnityEngine.Object[]> singleAsset = null;
            public WeakReference<UnityEngine.Object[]> allAssets = null;

            public WeakReference<UnityEngine.Object> singleAssetRef = null;     // used for cache referenced target
            public WeakList<UnityEngine.Object> mutiAssetsRef = null;           // used for cache referenced targets

            private WeakReference<UnityEngine.Object[]> _allObjs = null;   // cached type change

            public bool unloadable = true;
            public bool allAssetsLoaded { get { return allAssets != null; } }
            public bool anyUnloaded = false;
            public System.Action<System.Type, int> onUnload = null;

            public TypeData() { }

            #region Main Funcs
            public TypeData Set(Type type, UnityEngine.Object[] value, bool isAll)
            {
                this.type = type;
                anyUnloaded = false;
                if(isAll)
                {
                    SetAsset(ref allAssets, value);
                    SetAsset(ref mutiAssetsRef, value);
                }
                else
                {
                    SetAsset(ref singleAsset, value);
                    SetAsset(ref singleAssetRef, value);
                }
                return this;
            }

            // get single can't ignor all assets
            public UnityEngine.Object GetObject()
            {
                if(singleAssetRef != null)
                {
                    var obj = singleAssetRef.Object;
                    if(obj)
                    {
                        return obj;
                    }
                }
                if(mutiAssetsRef != null && mutiAssetsRef.Count > 0)
                {
                    return mutiAssetsRef[0];
                }
                return null;
            }
            // get all can ignor single asset
            public UnityEngine.Object[] GetAllObjects()
            {
                if(mutiAssetsRef != null && mutiAssetsRef.Count > 0)
                {
                    if(_allObjs == null)
                    {
                        _allObjs = WeakReference<UnityEngine.Object[]>.Allocate();
                    }
                    if(_allObjs.Target == null)
                    {
                        var objs = Utility.CreateAssetArray(type, mutiAssetsRef.Count);   // must use CreateInstance for type casting
                        mutiAssetsRef.CopyTo(objs, 0);
                        _allObjs.SetTarget(objs);
                    }
                    return _allObjs.Object;
                }
                return null;
            }

            public bool Contains(UnityEngine.Object target)
            {
                if(singleAssetRef != null)
                {
                    if(singleAssetRef.Object == target)
                    {
                        return true;
                    }
                }
                if(mutiAssetsRef != null)
                {
                    if(mutiAssetsRef.Contains(target))
                    {
                        return true;
                    }
                }
                return false;
            }

            public bool Unload(UnityEngine.Object target)
            {
                bool unloaded = false;
                if(target)
                {
                    if(singleAssetRef != null && singleAssetRef.Object == target)
                    {
                        unloaded = true;
                        if(onUnload != null)
                        {
                            onUnload.Invoke(type, singleAssetRef.hash);
                        }
                        singleAssetRef.Object = null;
                    }
                    if(mutiAssetsRef != null)
                    {
                        int hash = 0;
                        if(mutiAssetsRef.Replace(target, null, ref hash) >= 0)
                        {
                            if(onUnload != null)
                            {
                                onUnload.Invoke(type, hash);
                            }
                            unloaded = true;
                        }
                    }
                }
                if(unloaded && NoAsset())
                {
                    UnloadAll();
                }
                return unloaded;
            }

            public void UnloadAll()
            {
                if(singleAsset != null)
                {
                    AssetLoadManager.Instance.UnloadAsset(singleAsset.hash);
                }
                if(allAssets != null)
                {
                    AssetLoadManager.Instance.UnloadAsset(allAssets.hash);
                }
                // asset bundle will be Unload(true)
                if(unloadable)
                {
                    if(mutiAssetsRef != null)
                    {
                        WeakList<UnityEngine.Object>.DeAllocate(ref mutiAssetsRef);
                    }
                    if(singleAssetRef != null)
                    {
                        WeakReference<UnityEngine.Object>.DeAllocate(ref singleAssetRef);
                    }
                    if(_allObjs != null)
                    {
                        WeakReference<UnityEngine.Object[]>.DeAllocate(ref _allObjs);
                    }
                }
            }

            public void Clear()
            {
                type = null;
                unloadable = true;
                anyUnloaded = false;
                WeakReference<UnityEngine.Object[]>.DeAllocate(ref singleAsset);
                WeakReference<UnityEngine.Object[]>.DeAllocate(ref allAssets);
                WeakReference<UnityEngine.Object>.DeAllocate(ref singleAssetRef);
                WeakList<UnityEngine.Object>.DeAllocate(ref mutiAssetsRef);
                WeakReference<UnityEngine.Object[]>.DeAllocate(ref _allObjs);
            }
            #endregion

            #region Help Funcs
            public bool NoAsset()
            {
                if(singleAssetRef != null && singleAssetRef.Target != null)
                {
                    return false;
                }
                if(mutiAssetsRef != null)
                {
                    for(int i = 0; i < mutiAssetsRef.Count; i++)
                    {
                        if(mutiAssetsRef[i])
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            // if asset lost return false
            public bool CheckAssetNotLost(bool checkAll)
            {
                if(anyUnloaded)
                {
                    return false;
                }
                if(checkAll)
                {
                    if(mutiAssetsRef != null && mutiAssetsRef.Count > 0)
                    {
                        for(int i = 0; i < mutiAssetsRef.Count; i++)
                        {
                            var asset = mutiAssetsRef[i];
                            if(asset == false)
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
                else
                {
                    if(singleAssetRef != null)
                    {
                        var asset = singleAssetRef.Object;
                        return asset;
                    }
                }
                return false;
            }
            public bool IsAllAssetsLost()
            {
                if(singleAssetRef != null && singleAssetRef.Object)
                {
                    return false;
                }
                if(mutiAssetsRef != null && mutiAssetsRef.Count > 0)
                {
                    for(int i = 0; i < mutiAssetsRef.Count; i++)
                    {
                        var asset = mutiAssetsRef[i];
                        if(asset)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            private void SetAsset(ref WeakReference<UnityEngine.Object[]> weakRef, UnityEngine.Object[] value)
            {
                if(weakRef == null)
                {
                    weakRef = WeakReference<UnityEngine.Object[]>.Allocate(value);
                }
                weakRef.SetTarget(value);
            }
            private void SetAsset(ref WeakList<UnityEngine.Object> weakList, UnityEngine.Object[] value)
            {
                if(weakList == null)
                {
                    weakList = WeakList<UnityEngine.Object>.Allocate();
                }
                else
                {
                    weakList.Clear();
                }
                weakList.AddRange(value);
            }
            private void SetAsset(ref WeakReference<UnityEngine.Object> weakRef, UnityEngine.Object[] value)
            {
                if(weakRef == null)
                {
                    weakRef = WeakReference<UnityEngine.Object>.Allocate();
                }
                weakRef.SetTarget(value != null && value.Length > 0 ? value[0] : null);
            }
            #endregion
        }
        #endregion

        // Resource reference pool is totally weakreferences.
        private static Dictionary<string, List<TypeData>> _pool = new Dictionary<string, List<TypeData>>();
        private static Dictionary<System.Type, Dictionary<int, TypeData>> _resInv = new Dictionary<System.Type, Dictionary<int, TypeData>>();
        private static System.Action<System.Type, int> _onAssetUnload = null;

        static ResourcePool()
        {
            AssetBundleMaster.AssetLoad.AssetList.onAssetListLoaded = new Action<AssetList>(ResourcePool.OnLoaded);
            AssetBundleMaster.AssetLoad.AssetLoadManager.onResourcesUnloaded += ResourcePool.CheckUnloadedAssets;
            _onAssetUnload = new System.Action<System.Type, int>(OnSingleAssetUnload);
            TypeDataPool.Set(() => { return new TypeData() { onUnload = _onAssetUnload }; }, (_typeData) => { _typeData.Clear(); });
            TypeDataListPool.Set(() => { return new List<TypeData>(); }, (_typeDatas) => { _typeDatas.Clear(); });
        }


        #region Main Funcs
        /// <summary>
        /// Load asset from a path. 
        /// Notice :    if in Resources mode, the loadPath can't be a directory path, 
        ///             and in other mode pass a directory path will load the first asset in the folder
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="systemTypeInstance"></param>
        /// <returns></returns>
        public static UnityEngine.Object Load(string loadPath, System.Type systemTypeInstance, bool unloadable)
        {
            var loaded = GetAssetFromCache(loadPath, systemTypeInstance, unloadable);
            if(loaded)
            {
                return loaded;
            }
            var assets = AssetLoadManager.Instance.LoadAssets(loadPath, systemTypeInstance, unloadable);
            return (assets != null && assets.Length > 0) ? assets[0] : null;
        }

        /// <summary>
        /// Load all assets from a path. 
        /// Notice :    if pass a directory path, it will load all target type assets from the folder,
        ///             and if pass a file path(without ext name), it will load all target type assets with the same file name
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="systemTypeInstance"></param>
        /// <returns></returns>
        public static UnityEngine.Object[] LoadAll(string loadPath, System.Type systemTypeInstance, bool unloadable)
        {
            var loadedObjs = GetAllAssetsFromCache(loadPath, systemTypeInstance, unloadable);
            if(loadedObjs != null)
            {
                return loadedObjs;
            }
            var assets = AssetLoadManager.Instance.LoadAllAssets(loadPath, systemTypeInstance, unloadable);
            return assets;
        }

        /// <summary>
        /// Load asset from a path. 
        /// Notice :    if in Resources mode, the loadPath can't be a directory path, 
        ///             and in other mode pass a directory path will load the first asset in the folder
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="systemTypeInstance"></param>
        /// <param name="loaded"></param>
        public static void LoadAsync(string loadPath, System.Type systemTypeInstance, System.Action<UnityEngine.Object> loaded, bool unloadable)
        {
            var obj = GetAssetFromCache(loadPath, systemTypeInstance, unloadable);
            if(obj)
            {
                if(loaded != null)
                {
                    loaded.Invoke(obj);
                }
                return;
            }
            var call = (loaded != null) ? new System.Action<UnityEngine.Object[]>((_assets) =>
            {
                var tagObj = (_assets != null && _assets.Length > 0) ? _assets[0] : null;
                if(loaded != null)
                {
                    loaded(tagObj);
                }
            }) : null;
            AssetLoadManager.Instance.LoadAssetsAsync(loadPath, systemTypeInstance, unloadable, call);
        }

        /// <summary>
        /// Load all assets from a path. 
        /// Notice :    if pass a directory path, it will load all target type assets from the folder,
        ///             and if pass a file path(without ext name), it will load all target type assets with the same file name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath"></param>
        /// <param name="loaded"></param>
        public static void LoadAllAsync(string loadPath, System.Type systemTypeInstance, System.Action<UnityEngine.Object[]> loaded, bool unloadable)
        {
            var loadedObjs = GetAllAssetsFromCache(loadPath, systemTypeInstance, unloadable);
            if(loadedObjs != null)
            {
                if(loaded != null)
                {
                    loaded.Invoke(loadedObjs);
                }
                return;
            }
            AssetLoadManager.Instance.LoadAllAssetsAsync(loadPath, systemTypeInstance, unloadable, loaded);
        }

        /// <summary>
        /// This logic is just for compatible with Resources.LoadAsync(...), recommand to use 
        /// LoadAsync(string loadPath, System.Type systemTypeInstance, System.Action<UnityEngine.Object> loaded) instead!!!
        /// It works but no efficiency
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="systemTypeInstance"></param>
        /// <returns></returns>
        public static AssetBundleMaster.ResourceLoad.ResourceRequest LoadAsync(string loadPath, System.Type systemTypeInstance, bool unloadable)
        {
            var obj = GetAssetFromCache(loadPath, systemTypeInstance, unloadable);
            if(obj)
            {
                return new AssetBundleMaster.ResourceLoad.ResourceRequest().Set(obj);
            }
            var loader = AssetLoadManager.Instance.LoadAssetsAsync(loadPath, systemTypeInstance, unloadable, null);
            return new AssetBundleMaster.ResourceLoad.ResourceRequest().Set(loader);
        }

        /// <summary>
        /// Unload Asset with target type and load path, Recommand use this API to unload assets in AssetBundle Mode
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="systemTypeInstance"></param>
        /// <param name="inherit"></param>
        public static void UnloadAsset(string loadPath, System.Type systemTypeInstance, bool inherit = false)
        {
            var typeDatas = _pool.TryGetValue(loadPath);
            if(typeDatas != null)
            {
                for(int i = typeDatas.Count - 1; i >= 0; i--)
                {
                    var typeData = typeDatas[i];
                    if(typeData.type == systemTypeInstance || (inherit && typeData.type.IsSubclassOf(systemTypeInstance)))
                    {
                        typeData.UnloadAll();
                    }
                }
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else
            {
                Debug.LogWarning("This asset is no exists:" + loadPath);
            }
#endif
        }

        /// <summary>
        /// Unload asset with no type define, this is a inaccurate API
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="inherit"></param>
        public static void UnloadAsset(UnityEngine.Object asset)
        {
            if(asset)
            {
                UnloadAsset(asset, asset.GetType());
            }
        }

        /// <summary>
        /// Unload Asset with target set type, incase error type
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="loadType"></param>
        public static void UnloadAsset(UnityEngine.Object asset, System.Type loadType)
        {
            if(asset)
            {
                var typeList = _resInv.TryGetValue(loadType);
                if(typeList != null)
                {
                    var typeData = typeList.TryGetValue(asset.GetHashCode());
                    if(typeData != null)
                    {
                        // unload target
                        if(typeData.Unload(asset))
                        {
                            typeData.anyUnloaded = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unload All Assets
        /// </summary>
        public static void UnloadAllAssets()
        {
            foreach(var typeDatas in _pool.Values)
            {
                foreach(var typeData in typeDatas)
                {
                    typeData.UnloadAll();
                }
            }
        }
        #endregion

        #region Help Funcs
        // loaded call
        public static void OnLoaded(AssetList assetList)
        {
            if(assetList != null)
            {
                ResourcePool.OnLoaded(assetList.loadPath, assetList.systemTypeInstance, assetList.Assets, assetList.loadAll, assetList.unloadable);
            }
        }
        // loaded call
        public static void OnLoaded(string loadPath, System.Type systemTypeInstance, UnityEngine.Object[] objs, bool allAssets, bool unloadable)
        {
            var typeDatas = _pool.GetValue(loadPath, () => { return TypeDataListPool.Allocate(); });
            var typeData = GetTargetTypeData(typeDatas, systemTypeInstance);
            if(typeData == null)
            {
                typeData = TypeDataPool.Allocate().Set(systemTypeInstance, objs, allAssets);
                typeDatas.Add(typeData);
            }
            else
            {
                typeData.Set(systemTypeInstance, objs, allAssets);
            }
            typeData.unloadable &= unloadable;
            if(objs != null && objs.Length > 0)
            {
                var list = _resInv.GetValue(systemTypeInstance, ()=> { return new Dictionary<int, TypeData>(); });
                foreach(var obj in objs)
                {
                    if(obj)
                    {
                        list[obj.GetHashCode()] = typeData;
                    }
                }
            }
        }
        // get loaded assets
        public static UnityEngine.Object GetAssetFromCache(string loadPath, System.Type systemTypeInstance, bool unloadable)
        {
            var typeDatas = _pool.TryGetValue(loadPath);
            if(typeDatas != null)
            {
                var typeData = GetTargetTypeData(typeDatas, systemTypeInstance);
                if(typeData != null)
                {
                    typeData.unloadable &= unloadable;
                    return typeData.GetObject();
                }
            }
            return null;
        }
        // check is load all logic is already loaded
        public static UnityEngine.Object[] GetAllAssetsFromCache(string loadPath, System.Type systemTypeInstance, bool unloadable)
        {
            var typeDatas = _pool.TryGetValue(loadPath);
            if(typeDatas != null)
            {
                var typeData = GetTargetTypeData(typeDatas, systemTypeInstance);
                if(typeData != null && typeData.allAssetsLoaded && typeData.CheckAssetNotLost(true))
                {
                    typeData.unloadable &= unloadable;
                    return typeData.GetAllObjects();
                }
            }
            return null;
        }
        // get type data from list
        public static TypeData GetTargetTypeData(List<TypeData> typeDatas, System.Type type)
        {
            if(typeDatas != null && typeDatas.Count > 0)
            {
                for(int i = 0; i < typeDatas.Count; i++)
                {
                    var tempTypeData = typeDatas[i];
                    if(tempTypeData.type == type)
                    {
                        return tempTypeData;
                    }
                }
            }
            return null;
        }
        // check unloaded caches
        private static void CheckUnloadedAssets(HashSet<string> unloadedAssets)
        {
            if(unloadedAssets.Count > 0)
            {
                foreach(var assetFile in unloadedAssets)
                {
                    var list = _pool.TryGetValue(assetFile);
                    if(list != null && list.Count > 0)
                    {
                        list.RemoveAll((_typeData) =>
                        {
                            if(_typeData.IsAllAssetsLost())
                            {
                                TypeDataPool.DeAllocate(_typeData);
                                return true;
                            }
                            return false;
                        });
                        if(list.Count == 0)
                        {
                            _pool.Remove(assetFile);
                            TypeDataListPool.DeAllocate(list);
                        }
                    }
                }
            }

        }
        // remove unloaded inverse reference
        private static void OnSingleAssetUnload(System.Type type, int hash)
        {
            var list = _resInv.TryGetValue(type);
            if(list != null)
            {
                list.Remove(hash);
            }
        }
        #endregion
    }

    /// <summary>
    /// A CustomYieldInstruction that implement for usage of Resources.LoadAsync(...)
    /// </summary>
    public class ResourceRequest : CustomYieldInstruction
    {
        public AssetLoaderBase loader { get; set; }
        public WeakReference assetObj;
        public UnityEngine.Object asset
        {
            get
            {
                if(assetObj != null)
                {
                    return assetObj.Target as UnityEngine.Object;
                }
                if(loader == null || loader.Assets == null)
                {
                    return null;
                }
                if(loader.Assets.Length > 0)
                {
                    return loader.Assets[0];
                }
                return null;
            }
        }
        public bool isDone
        {
            get
            {
                return assetObj != null || (loader != null && loader.isDone);
            }
        }

        public override bool keepWaiting
        {
            get
            {
                return isDone == false;
            }
        }

        public ResourceRequest Set(AssetLoaderBase loader)
        {
            this.loader = loader;
            return this;
        }
        public ResourceRequest Set(UnityEngine.Object obj)
        {
            assetObj = new WeakReference(obj);
            return this;
        }
    }
}
