using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AssetBundleMaster.Example
{
    using AssetBundleMaster.ResourceLoad;

    public class Example_UnloadScene : MonoBehaviour
    {
        public Button LoadSceneAsyncButton;
        public Button UnloadSceneButton;
        public Button UnloadSceneAssetButton;
        public Button UnloadLoadingSceneButton;
        [Space(10.0f)]
        public InputField UnloadLoadingSceneWaitFrame;

        public const string SceneLoadPath = "Scenes/TestScene1/TestScene";

        private List<int> m_loadedScenes = new List<int>();

        // Use this for initialization
        void Start()
        {
            UnloadLoadingSceneWaitFrame.contentType = InputField.ContentType.IntegerNumber;

            LoadSceneAsyncButton.onClick.AddListener(() =>
            {
                LoadOneScene();
            });

            UnloadSceneButton.onClick.AddListener(() =>
            {
                UnloadOneScene();
            });

            UnloadLoadingSceneButton.onClick.AddListener(() =>
            {
                LoadOneScene();

                int waitFrame = 0;
                if(UnloadLoadingSceneWaitFrame)
                {
                    int.TryParse(UnloadLoadingSceneWaitFrame.text, out waitFrame);
                }

                StartCoroutine(Wait(waitFrame, () =>
                {
                    UnloadOneScene();
                }));
            });

            StartScene.RegisterClickFocus();
        }

        private void OnDestroy()
        {
            foreach(var id in m_loadedScenes)
            {
                SceneLoadManager.Instance.UnloadScene(id);
            }
            m_loadedScenes.Clear();
        }

        void LoadOneScene()
        {
            Debug.Log("Start Load scene[" + SceneLoadPath + "] at frame : " + Time.frameCount);

            var id = SceneLoadManager.Instance.LoadScene(SceneLoadPath,
                AssetLoad.LoadThreadMode.Asynchronous,
                UnityEngine.SceneManagement.LoadSceneMode.Additive,
                (_id, _scene) =>
                {
                    Debug.Log("Scene [" + _scene.name + "] Loaded at frame : " + Time.frameCount);
                });
            m_loadedScenes.Add(id);
        }

        void UnloadOneScene()
        {
            if(m_loadedScenes.Count > 0)
            {
                var sceneId = m_loadedScenes[0];
                m_loadedScenes.RemoveAt(0);
                Debug.Log("Request unload at frame : " + Time.frameCount);
                // Please Unload Scene In This Way
                SceneLoadManager.Instance.UnloadScene(sceneId, (_id, _scene) =>
                {
                    Debug.Log("Unloaded " + _id + " [" + _scene.path + "] at frame : " + Time.frameCount);
                });
            }
        }

        IEnumerator Wait(int frames, System.Action call)
        {
            while(frames >= 0)
            {
                yield return new WaitForEndOfFrame();
                frames--;
            }
            call();
        }
    }


}