using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace AssetBundleMaster.ResourceLoad
{
    using AssetBundleMaster.Common;

    /// <summary>
    /// This is the high level API for loading any resources
    /// </summary>
    public class ResourceLoadManager : Singleton<ResourceLoadManager>
    {
        public static bool atlasLoadImmediate = true;

        protected override void Initialize()
        {
#if UNITY_2017_1_OR_NEWER
            UnityEngine.U2D.SpriteAtlasManager.atlasRequested += RequestAtlas;
#endif
        }
        protected override void UnInitialize()
        {
#if UNITY_2017_1_OR_NEWER
            UnityEngine.U2D.SpriteAtlasManager.atlasRequested -= RequestAtlas;
#endif
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
        public UnityEngine.Object Load(string loadPath, System.Type systemTypeInstance)
        {
            return ResourcePool.Load(loadPath, systemTypeInstance, false);
        }
        /// <summary>
        /// Load asset from a path. 
        /// Notice :    if in Resources mode, the loadPath can't be a directory path, 
        ///             and in other mode pass a directory path will load the first asset in the folder
        /// </summary>
        /// <param name="loadPath"></param>
        /// <returns></returns>
        public UnityEngine.Object Load(string loadPath)
        {
            return Load(loadPath, typeof(UnityEngine.Object));
        }
        /// <summary>
        /// Load asset from a path. 
        /// Notice :    if in Resources mode, the loadPath can't be a directory path, 
        ///             and in other mode pass a directory path will load the first asset in the folder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath"></param>
        /// <returns></returns>
        public T Load<T>(string loadPath) where T : UnityEngine.Object
        {
            return Load(loadPath, typeof(T)) as T;
        }

        /// <summary>
        /// Load all assets from a path. 
        /// Notice :    if pass a directory path, it will load all target type assets from the folder,
        ///             and if pass a file path(without ext name), it will load all target type assets with the same file name
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="systemTypeInstance"></param>
        /// <returns></returns>
        public UnityEngine.Object[] LoadAll(string loadPath, System.Type systemTypeInstance)
        {
            return ResourcePool.LoadAll(loadPath, systemTypeInstance, false);
        }
        /// <summary>
        /// Load all assets from a path. 
        /// Notice :    if pass a directory path, it will load all target type assets from the folder,
        ///             and if pass a file path(without ext name), it will load all target type assets with the same file name
        /// </summary>
        /// <param name="loadPath"></param>
        /// <returns></returns>
        public UnityEngine.Object[] LoadAll(string loadPath)
        {
            return LoadAll(loadPath, typeof(UnityEngine.Object));
        }
        /// <summary>
        /// Load all assets from a path. 
        /// Notice :    if pass a directory path, it will load all target type assets from the folder,
        ///             and if pass a file path(without ext name), it will load all target type assets with the same file name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath"></param>
        /// <returns></returns>
        public T[] LoadAll<T>(string loadPath) where T : UnityEngine.Object
        {
            return LoadAll(loadPath, typeof(T)) as T[];
        }

        /// <summary>
        /// Load asset from a path. 
        /// Notice :    if in Resources mode, the loadPath can't be a directory path, 
        ///             and in other mode pass a directory path will load the first asset in the folder
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="systemTypeInstance"></param>
        /// <param name="loaded"></param>
        public void LoadAsync(string loadPath, System.Type systemTypeInstance, System.Action<UnityEngine.Object> loaded)
        {
            ResourcePool.LoadAsync(loadPath, systemTypeInstance, loaded, false);
        }
        /// <summary>
        /// Load asset from a path. 
        /// Notice :    if in Resources mode, the loadPath can't be a directory path, 
        ///             and in other mode pass a directory path will load the first asset in the folder
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="loaded"></param>
        public void LoadAsync(string loadPath, System.Action<UnityEngine.Object> loaded)
        {
            LoadAsync(loadPath, typeof(UnityEngine.Object), loaded);
        }
        /// <summary>
        /// Load asset from a path. 
        /// Notice :    if in Resources mode, the loadPath can't be a directory path, 
        ///             and in other mode pass a directory path will load the first asset in the folder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath"></param>
        /// <param name="loaded"></param>
        public void LoadAsync<T>(string loadPath, System.Action<T> loaded) where T : UnityEngine.Object
        {
            LoadAsync(loadPath, typeof(T), (_loadedAssets) =>
            {
                if(loaded != null)
                {
                    loaded.Invoke(_loadedAssets as T);
                }
            });
        }

        /// <summary>
        /// Load all assets from a path. 
        /// Notice :    if pass a directory path, it will load all target type assets from the folder,
        ///             and if pass a file path(without ext name), it will load all target type assets with the same file name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath"></param>
        /// <param name="loaded"></param>
        private void LoadAllAsync(string loadPath, System.Type systemTypeInstance, System.Action<UnityEngine.Object[]> loaded)
        {
            ResourcePool.LoadAllAsync(loadPath, systemTypeInstance, loaded, false);
        }
        /// <summary>
        /// Load all assets from a path. 
        /// Notice :    if pass a directory path, it will load all target type assets from the folder,
        ///             and if pass a file path(without ext name), it will load all target type assets with the same file name
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="loaded"></param>
        public void LoadAllAsync(string loadPath, System.Action<UnityEngine.Object[]> loaded)
        {
            LoadAllAsync(loadPath, typeof(UnityEngine.Object), loaded);
        }
        /// <summary>
        /// Load all assets from a path. 
        /// Notice :    if pass a directory path, it will load all target type assets from the folder,
        ///             and if pass a file path(without ext name), it will load all target type assets with the same file name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath"></param>
        /// <param name="loaded"></param>
        public void LoadAllAsync<T>(string loadPath, System.Action<T[]> loaded) where T : UnityEngine.Object
        {
            LoadAllAsync(loadPath, typeof(T), (_loadedAssets) =>
            {
                if(loaded != null)
                {
                    loaded.Invoke(_loadedAssets as T[]);
                }
            });
        }

        /// <summary>
        /// This logic is just for compatible with Resources.LoadAsync(...), recommand to use 
        /// LoadAsync(string loadPath, System.Type systemTypeInstance, System.Action<UnityEngine.Object> loaded) instead!!!
        /// It works but no efficiency
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="systemTypeInstance"></param>
        /// <returns></returns>
        public AssetBundleMaster.ResourceLoad.ResourceRequest LoadAsync(string loadPath, System.Type systemTypeInstance)
        {
            return ResourcePool.LoadAsync(loadPath, systemTypeInstance, false);
        }
        /// <summary>
        /// This logic is just for compatible with Resources.LoadAsync(...), recommand to use 
        /// LoadAsync(string loadPath, System.Action<UnityEngine.Object> loaded) instead!!!
        /// It works but no efficiency
        /// </summary>
        /// <param name="loadPath"></param>
        /// <returns></returns>
        public AssetBundleMaster.ResourceLoad.ResourceRequest LoadAsync(string loadPath)
        {
            return LoadAsync(loadPath, typeof(UnityEngine.Object));
        }
        /// <summary>
        /// This logic is just for compatible with Resources.LoadAsync(...), recommand to use 
        /// LoadAsync<T>(string loadPath, System.Action<T> loaded) instead!!!
        /// It works but no efficiency
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath"></param>
        /// <returns></returns>
        public AssetBundleMaster.ResourceLoad.ResourceRequest LoadAsync<T>(string loadPath) where T : UnityEngine.Object
        {
            return LoadAsync(loadPath, typeof(T));
        }

        /// <summary>
        /// Unload Asset with target type and load path, Recommand use this API to unload assets in AssetBundle Mode
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="systemTypeInstance"> target type </param>
        /// <param name="inherit"> if inherit is true, unload all types that IsSubclassOf systemTypeInstance </param>
        public void UnloadAsset(string loadPath, System.Type systemTypeInstance, bool inherit = false)
        {
            ResourcePool.UnloadAsset(loadPath, systemTypeInstance, inherit);
        }
        /// <summary>
        /// Unload Asset with target type and load path, generic version
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="loadPath"> target type </param>
        /// <param name="inherit"> if inherit is true, unload all types that IsSubclassOf systemTypeInstance </param>
        public void UnloadAsset<T>(string loadPath, bool inherit = false) where T : UnityEngine.Object
        {
            UnloadAsset(loadPath, typeof(T), inherit);
        }
        /// <summary>
        /// Unload Asset target, inaccurate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="inherit"></param>
        public void UnloadAsset(UnityEngine.Object target)
        {
            ResourcePool.UnloadAsset(target);
        }
        /// <summary>
        /// Unload Asset target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="type"></param>
        public void UnloadAsset(UnityEngine.Object target, System.Type type)
        {
            ResourcePool.UnloadAsset(target, type);
        }

        /// <summary>
        /// Unload All Assets
        /// </summary>
        public void UnloadAllAssets()
        {
            ResourcePool.UnloadAllAssets();
        }
        #endregion

        #region Help Funcs
#if UNITY_2017_1_OR_NEWER
        // Sprite load is kind of special, it happened when an auto binding logic not working
        private void RequestAtlas(string tag, System.Action<UnityEngine.U2D.SpriteAtlas> callback)
        {
            bool syncMode = atlasLoadImmediate;
            if(AssetBundleMaster.AssetLoad.GameConfig.Instance.resourceLoadMode == AssetLoad.ResourcesLoadMode.AssetBundle_Remote)
            {
                syncMode = false;
            }
            var path = string.Concat(AssetBundleMaster.AssetLoad.GameConfig.SpriteAtlasFolderEditor, "/", tag);
            if(syncMode)
            {
                var spriteAtlas = Load<UnityEngine.U2D.SpriteAtlas>(path);    // use late bind logic, this is why we set atlas in the same folder
                callback(spriteAtlas);
            }
            else
            {
                LoadAsync<UnityEngine.U2D.SpriteAtlas>(path, callback);
            }
        }
#endif
        #endregion

    }
}

