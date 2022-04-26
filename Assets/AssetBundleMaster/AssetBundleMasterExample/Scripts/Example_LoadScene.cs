using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AssetBundleMaster.Example
{
    using AssetBundleMaster.ResourceLoad;
    using AssetBundleMaster.Common;
    using AssetBundleMaster.AssetLoad;

    public class Example_LoadScene : MonoBehaviour
    {
        public Button LoadSceneSyncButton1;
        public Button LoadSceneAsyncButton2;

        public Button UnLoadAllScenes;

        public const string SceneLoadPath1 = "Scenes/TestScene1/TestScene";
        public const string SceneLoadPath2 = "Scenes/TestScene2/TestScene";

        public List<int> m_loadedScenes = new List<int>();

        // Use this for initialization
        void Start()
        {
            LoadSceneSyncButton1.onClick.AddListener(() =>
            {
                LoadOneScene(SceneLoadPath1, AssetLoad.LoadThreadMode.Synchronous);
            });
            LoadSceneAsyncButton2.onClick.AddListener(() =>
            {
                LoadOneScene(SceneLoadPath2, AssetLoad.LoadThreadMode.Asynchronous);
            });

            UnLoadAllScenes.onClick.AddListener(() =>
            {
                Debug.Log("Request unload at frame : " + Time.frameCount);
                for(int i = 0, imax = m_loadedScenes.Count; i < imax; i++)
                {
                    var sceneId = m_loadedScenes[i];
                    SceneLoadManager.Instance.UnloadScene(sceneId, (_id, _scene) =>
                    {
                        Debug.Log("Scene Unloaded ID " + _id + " [" + _scene.name + "] at frame : " + Time.frameCount);
                    });
                }
                m_loadedScenes.Clear();
            });

            StartScene.RegisterClickFocus();
        }

        private void OnDestroy()
        {
            foreach(var id in m_loadedScenes)
            {
                SceneLoadManager.Instance.UnloadScene(id);        // low-level API
            }
            m_loadedScenes.Clear();
        }

        void LoadOneScene(string loadPath, AssetLoad.LoadThreadMode mode)
        {
            Debug.Log("Start Load scene[" + loadPath + "] at frame : " + Time.frameCount);

            var id = SceneLoadManager.Instance.LoadScene(loadPath,
                mode,
                UnityEngine.SceneManagement.LoadSceneMode.Additive,
                (_id, _scene) =>
                {
                    Debug.Log("Scene [" + _scene.path + "] Loaded at frame : " + Time.frameCount);
                });
            m_loadedScenes.Add(id);
        }

    }


}