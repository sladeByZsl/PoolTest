using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssetBundleMaster.Example
{
    using AssetBundleMaster.ResourceLoad;

    public class Example_SpawnDespawn : MonoBehaviour
    {
        public Button SpawnButton;
        public Button DespawnButton;

        public int spawnCount = 5;
        public int despawnCount = 10;
        public List<GameObject> instantiatedGameObjects = new List<GameObject>();

        public const string LoadPath = "Prefabs/Cube";

        // Use this for initialization
        void Start()
        {
            SpawnButton.onClick.AddListener(() =>
            {
                Debug.Log("Start Load at frame : " + Time.frameCount);
                CreateCube(LoadPath, null, spawnCount, (_cube, _index) =>
                {
                    _cube.name = "Cube_" + _index;
                    instantiatedGameObjects.Add(_cube);
                });
            });
            DespawnButton.onClick.AddListener(() =>
            {
                for(int i = 0, imax = Mathf.Min(instantiatedGameObjects.Count, despawnCount); i < imax; i++)
                {
                    var go = instantiatedGameObjects[0];
                    instantiatedGameObjects.RemoveAt(0);
                    PrefabLoadManager.Instance.Despawn(go);
                }
            });
        }

        private void CreateCube(string loadPath, string poolName, int count, System.Action<GameObject, int> created = null)
        {
            // you can spawn it directly if not in remote mode
            if(AssetBundleMaster.AssetLoad.AssetLoadManager.Instance.configs.isRemoteAssets == false)
            {
                for(int i = 0; i < count; i++)
                {
                    var cube = PrefabLoadManager.Instance.Spawn(loadPath, poolName);
                    SetCubeInfo(cube);
                    if(created != null)
                    {
                        created.Invoke(cube, i);
                    }
                }
            }
            else
            {
                // or you can use Async load at any time
                for(int i = 0; i < count; i++)
                {
                    int index = i;
                    PrefabLoadManager.Instance.SpawnAsync(loadPath, (_go) =>
                    {
                        SetCubeInfo(_go);
                        if(created != null)
                        {
                            created.Invoke(_go, index);
                        }
                    }, poolName);
                }

                // if you care about performance, use PrefabLoadManager.Instance.LoadAssetToPoolAsync and spawn it after asset loaded
                // For example : 
                //PrefabLoadManager.Instance.LoadAssetToPoolAsync(loadPath, (_prefab, _pool) =>
                //{
                //    for(int i = 0; i < count; i++)
                //    {
                //        var cube = PrefabLoadManager.Instance.Spawn(loadPath, poolName);
                //        SetCubeInfo(cube);
                //        if(created != null)
                //        {
                //            created.Invoke(cube, i);
                //        }
                //    }
                //}, poolName);
            }
        }

        private void SetCubeInfo(GameObject cube)
        {
            cube.transform.position = new Vector3(UnityEngine.Random.Range(-3f, 3f),
                UnityEngine.Random.Range(-2f, 4f),
                UnityEngine.Random.Range(-5f, 5f));
            cube.transform.rotation = UnityEngine.Random.rotation;

            Debug.Log(cube + " Instantiated at frame : " + Time.frameCount);

#if UNITY_EDITOR
            UnityEditor.Selection.activeGameObject = cube;
#endif
        }
    }
}