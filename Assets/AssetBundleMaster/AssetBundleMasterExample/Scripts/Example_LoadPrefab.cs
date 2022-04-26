using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssetBundleMaster.Example
{
    using AssetBundleMaster.ResourceLoad;
    using AssetBundleMaster.Common;

    public class Example_LoadPrefab : MonoBehaviour
    {
        public const string LoadPath = "Prefabs/Cube";

        public Button LoadButton;
        public Button LoadAsyncButton1;
        public Button LoadAsyncButton2;

        public int spawnCount = 15;

        // Use this for initialization
        void Start()
        {
            LoadButton.onClick.AddListener(() =>
            {
                for(int i = 0; i < spawnCount; i++)
                {
                    var cube = PrefabLoadManager.Instance.Spawn(LoadPath, "SyncPool");  // spawn Sync
                    RandomCubePos(cube);
                }
            });

            /* 
            * These are two kinds of Async spawn way, The first (LoadAsyncButton1) is take a little overhead GC, 
            * ortherwise 2nd (LoadAsyncButton2) is less GC, see what is diferent
            */
            LoadAsyncButton1.onClick.AddListener(() =>
            {
                Debug.Log("Start Load at frame : " + Time.frameCount);
                for(int i = 0; i < spawnCount; i++)
                {
                    // if Asset not yet loaded, this will call multi times the asset load Low-Level API
                    PrefabLoadManager.Instance.SpawnAsync(LoadPath, (_cube) =>
                    {
                        RandomCubePos(_cube);
                        Debug.Log("Loaded at frame : " + Time.frameCount);

                    }, "AsyncPool");
                }
            });

            LoadAsyncButton2.onClick.AddListener(() =>
            {
                Debug.Log("Start Load at frame : " + Time.frameCount);
                // only call Low-Level API Once, wait until asset loaded
                PrefabLoadManager.Instance.LoadAssetToPoolAsync(LoadPath, (_prefab, _pool) =>
                {
                    for(int i = 0; i < spawnCount; i++)
                    {
                        var cube = _pool.Spawn(LoadPath, _prefab);      // you got the pool holds the Prefab
                        RandomCubePos(cube);
                        Debug.Log("Loaded at frame : " + Time.frameCount);
                    }

                }, "AsyncPool");

                
            });

            StartScene.RegisterClickFocus();
        }

        private void RandomCubePos(GameObject cube)
        {
            cube.transform.position = new Vector3(UnityEngine.Random.Range(-3f, 3f),
                UnityEngine.Random.Range(-2f, 4f),
                UnityEngine.Random.Range(-5f, 5f));
            cube.transform.rotation = UnityEngine.Random.rotation;

#if UNITY_EDITOR
            UnityEditor.Selection.activeGameObject = cube;
#endif
        }
    }
}