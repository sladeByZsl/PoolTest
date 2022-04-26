using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace AssetBundleMaster.ResourceLoad
{
    using AssetBundleMaster.Common;
    using AssetBundleMaster.AssetLoad;

    /// <summary>
    /// Scene load method is Async, no Sync Method!
    /// </summary>
    public class SceneLoadManager : Singleton<SceneLoadManager>
    {
        private HashSet<int> m_scenes = new HashSet<int>();

        #region Main Funcs
        /// <summary>
        /// Load Scene, the path is asset load path in editor, return the unique id of the scene
        /// </summary>
        /// <param name="sceneLoadPath"></param>
        /// <param name="loadMode"></param>
        /// <param name="loadSceneMode"></param>
        /// <param name="loaded"></param>
        /// <returns></returns>
        public int LoadScene(string sceneLoadPath,
            AssetBundleMaster.AssetLoad.LoadThreadMode loadMode = AssetBundleMaster.AssetLoad.LoadThreadMode.Asynchronous,
            UnityEngine.SceneManagement.LoadSceneMode loadSceneMode = LoadSceneMode.Single,
            System.Action<int, Scene> loaded = null)
        {
            if(loadSceneMode == LoadSceneMode.Single && m_scenes.Count > 0)
            {
                UnloadAllScenes();
            }
            int id = AssetBundleMaster.AssetLoad.AssetLoadManager.Instance.LoadLevel(sceneLoadPath, loadMode, loadSceneMode, (_id, _scene) =>
            {
                OnSceneLoaded(_id, _scene);
                if(loaded != null)
                {
                    loaded.Invoke(_id, _scene);
                }
            });
            m_scenes.Add(id);
            return id;
        }

        /// <summary>
        /// Unload a scene by unique ID
        /// unloadUnusedAssets will determin to call Resources.UnloadUnusedAssets() if the scene asset is not use any more
        /// </summary>
        /// <param name="id"></param>
        public void UnloadScene(int id, System.Action<int, Scene> unloaded = null)
        {
            m_scenes.Remove(id);
            AssetBundleMaster.AssetLoad.AssetLoadManager.Instance.UnloadLevel(id, unloaded);
        }

        /// <summary>
        /// Unload All loaded scenes
        /// </summary>
        public void UnloadAllScenes()
        {
            foreach(var id in m_scenes)
            {
                AssetBundleMaster.AssetLoad.AssetLoadManager.Instance.UnloadLevel(id);
            }
            m_scenes.Clear();
        }
        #endregion

        #region Help Funcs
        void OnSceneLoaded(int id, Scene scene)
        {
            m_scenes.Add(id);
        }
        #endregion

    }
}

