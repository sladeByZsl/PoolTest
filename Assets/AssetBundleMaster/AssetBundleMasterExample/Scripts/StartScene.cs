using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssetBundleMaster.Example
{
    using AssetBundleMaster.ResourceLoad;
    using AssetBundleMaster.AssetLoad;

    public class StartScene : MonoBehaviour
    {
        [SerializeField]
        public List<string> loadScenePaths = new List<string>();

        [SerializeField]
        public Dropdown dropdown;

        private HashSet<int> m_loadedScenes = new HashSet<int>();

        void Start()
        {
#if DEVELOPMENT_BUILD
            Application.runInBackground = false;
#else
            Application.runInBackground = true;
#endif

            if(dropdown)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(loadScenePaths);

                AssetLoadManager.Instance.minUnloadAssetCounter = 0;    // Set minUnloadAssetCounter by your self!!! 0 is just for Editor Testing!!!
                AssetLoadManager.Instance.unloadPaddingTime = 3f;     // Set unloadPaddingTime by your self!!! 1.5 is just for Editor Testing!!!
                AssetUnloadManager.Instance.maxUnloadPerFrame = 1f;

                // This Is The Game Entry Point!!!
                // if remote-assetbundle mode, we must wait for the AssetLoadManager to be inited(AssetBundleManifest & LocalVersion is also load from server), other modes you don't need it
                AssetLoadManager.Instance.OnAssetLoadModuleInited(() =>
                {
                    // start your game logic after AssetLoadModuleInited
                    dropdown.onValueChanged.AddListener(LoadLevel);
                    LoadLevel(0);
                });
            }
        }

        private void LoadLevel(int index)
        {
            try
            {
                var path = loadScenePaths[index];
                if(string.IsNullOrEmpty(path) == false)
                {
                    dropdown.interactable = false;
                    UnloadAllAssets();
                    SceneLoadManager.Instance.LoadScene(path,
                        LoadThreadMode.Asynchronous,
                        UnityEngine.SceneManagement.LoadSceneMode.Additive,
                        (_id, _scene) =>
                        {
                            dropdown.interactable = true;
                            m_loadedScenes.Add(_id);
                            FocusAssetLoadManager();
                        });
                }
            }
            catch { }
        }

        // Unload All Asset for Editor Profiler Testing
        private void UnloadAllAssets()
        {
            PrefabLoadManager.Instance.DestroyAllPools();
            ResourceLoadManager.Instance.UnloadAllAssets();
            foreach(var id in m_loadedScenes)
            {
                SceneLoadManager.Instance.UnloadScene(id);
            }
            m_loadedScenes.Clear();
        }

        public static void FocusAssetLoadManager()
        {
#if UNITY_EDITOR
            UnityEditor.Selection.activeGameObject = AssetBundleMaster.AssetLoad.AssetLoadManager.Instance.gameObject;
#endif
        }

        public static void RegisterClickFocus()
        {
#if UNITY_EDITOR
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if(scene.isLoaded)
            {
                foreach(var btn in GameObject.FindObjectsOfType<Button>())
                {
                    if(btn)
                    {
                        btn.onClick.AddListener(() =>
                        {
                            FocusAssetLoadManager();
                        });
                    }
                }
            }
#endif
        }
    }
}