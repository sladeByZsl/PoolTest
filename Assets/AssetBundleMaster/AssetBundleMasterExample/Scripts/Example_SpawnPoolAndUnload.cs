using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssetBundleMaster.Example
{
    using AssetBundleMaster.ResourceLoad;

    public class Example_SpawnPoolAndUnload : MonoBehaviour
    {
        public Button SpawnButton;
        public Button DestroyPoolButton1;
        public Button DestroyPoolButton2;
        public Button DestroyPoolButton3;
        public Button DestroyPoolAndUnloadButton;

        public string PoolName1 = "MyCubes[1]";
        public string PoolName2 = "MyCubes[2]";
        public string PoolName3 = "MyCubes[3]";

        public const string LoadPath = "Prefabs/Cube";

        private List<GameObject> m_caches2 = new List<GameObject>();
        private List<GameObject> m_caches3 = new List<GameObject>();

        // Use this for initialization
        void Start()
        {
            SpawnButton.onClick.AddListener(() =>
            {
                CreateCube(LoadPath, PoolName1, 12);
                CreateCube(LoadPath, PoolName2, 15, m_caches2);
                CreateCube(LoadPath, PoolName3, 10, m_caches3);
            });

            // how to destroy spawned targets
            DestroyPoolButton1.onClick.AddListener(() =>
            {
                PrefabLoadManager.Instance.DestroyTargetInPool(LoadPath, PoolName1, true);
            });
            DestroyPoolButton2.onClick.AddListener(() =>
            {
                foreach(var go in m_caches2)
                {
                    PrefabLoadManager.Instance.DestroySpawned(go, PoolName2, LoadPath, true);
                }
                m_caches2.Clear();
            });
            DestroyPoolButton3.onClick.AddListener(() =>
            {
                foreach(var go in m_caches3)
                {
                    PrefabLoadManager.Instance.DestroySpawned(go);
                }
                m_caches3.Clear();
            });

            // this will unload the asset
            DestroyPoolAndUnloadButton.onClick.AddListener(() =>
            {
                PrefabLoadManager.Instance.DestroyAllPools(true);
                StartScene.FocusAssetLoadManager();
            });
        }

        private void CreateCube(string loadPath, string poolName, int count, List<GameObject> caches = null)
        {
            for(int i = 0; i < count; i++)
            {
                var cube = PrefabLoadManager.Instance.Spawn(loadPath, poolName);
                cube.name = poolName + ":Cube" + i;
                cube.transform.position = new Vector3(UnityEngine.Random.Range(-3f, 3f),
                    UnityEngine.Random.Range(-2f, 4f),
                    UnityEngine.Random.Range(-5f, 5f));
                cube.transform.rotation = UnityEngine.Random.rotation;
                if(UnityEngine.Random.Range(0, 2) == 1)
                {
                    cube.transform.SetParent(null);
                }

                if(caches != null)
                {
                    caches.Add(cube);
                }

#if UNITY_EDITOR
                UnityEditor.Selection.activeGameObject = cube;
#endif
            }
        }
    }
}