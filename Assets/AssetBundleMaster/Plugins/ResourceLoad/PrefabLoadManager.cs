using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AssetBundleMaster.ResourceLoad
{
    using AssetBundleMaster.Common;
    using AssetBundleMaster.ObjectPool;
    using AssetBundleMaster.Extention;

    /// <summary>
    /// This is a high level API for control prefab assets, implemented object pool for the loaded prefab assets and control spawn/despawn
    /// Notice : most prefab is a GameObject as we know, we use a GameObject Pool as object pool, and prefab is an individual asset
    /// </summary>
    public class PrefabLoadManager : Singleton<PrefabLoadManager>
    {
        public const string DefaultGameObjectPoolName = "[DefaultGameObjectPool]";

        // GameObjectPool caches [PoolName, GameObjectPool]
        private Dictionary<string, GameObjectPool> _pools = new Dictionary<string, GameObjectPool>();
        // loaded prefab belongs pool lists [loadpath, pools]
        private Dictionary<string, HashSet<GameObjectPool>> _prefabBelongs = new Dictionary<string, HashSet<GameObjectPool>>();


        protected override void Initialize()
        {
            ResourceLoadManager.Create();
        }

        protected override void UnInitialize()
        {
            DestroyAllPools(true);
        }

        #region Main Funcs
        /// <summary>
        /// Spawn a Instantiated GameObject, aoto load prefab asset
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="poolName"></param>
        /// <returns></returns>
        public GameObject Spawn(string loadPath, string poolName = null, bool active = true)
        {
            GameObjectPool pool = null;
            var prefab = LoadAssetToPool(loadPath, out pool, poolName);
            if(prefab)
            {
                var go = pool.Spawn(loadPath, prefab, active);
                return go;
            }
            return null;
        }
        /// <summary>
        /// Spawn a Instantiated GameObject, load asset Async
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="loaded"></param>
        /// <param name="poolName"></param>
        public void SpawnAsync(string loadPath, System.Action<GameObject> loaded = null, string poolName = null, bool active = true)
        {
            LoadAssetToPoolAsync(loadPath, (_prefab, _pool) =>
            {
                if(_prefab && _pool)
                {
                    var go = _pool.Spawn(loadPath, _prefab, active);
                    if(loaded != null)
                    {
                        loaded.Invoke(go);
                    }
                }
            }, poolName);
        }

        /// <summary>
        /// Despawn an Instantiated GameObject, cache it to pool
        /// Notice : if poolName incorrect, it will search all pools to find the pool where it spawned from
        /// </summary>
        /// <param name="go"></param>
        /// <param name="poolName"></param>
        /// <returns></returns>
        public bool Despawn(GameObject go, string poolName = null)
        {
            if(go == false)
            {
                return false;
            }
            var pool = GetTargetSpawnedPool(go, poolName);
            if(pool && pool.Despawn(go))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Use for load asset, and set to target pool by poolName, just like ResourceLoadManager.Load
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="pool"></param>
        /// <param name="poolName"></param>
        /// <returns></returns>
        public GameObject LoadAssetToPool(string loadPath, out GameObjectPool pool, string poolName = null)
        {
            pool = GetPool(poolName, true);
            var prefab = pool.Get<GameObject>(loadPath);
            if(prefab == false)
            {
                prefab = ResourcePool.Load(loadPath, typeof(GameObject), true) as GameObject;
                OnAssetToPool(loadPath, pool, prefab);
            }
            return prefab;
        }
        /// <summary>
        /// Use for load asset, and set to target pool by poolName, just like ResourceLoadManager.LoadAsync
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="loaded"> Notice:GameObject is the prefab resource </param>
        /// <param name="poolName"></param>
        public void LoadAssetToPoolAsync(string loadPath, System.Action<GameObject, GameObjectPool> loaded = null, string poolName = null)
        {
            var pool = GetPool(poolName, true);
            var prefab = pool.Get<GameObject>(loadPath);
            if(prefab == false)
            {
                ResourcePool.LoadAsync(loadPath, typeof(GameObject), (_prefab) =>
                {
                    var loadedPrefab = _prefab as GameObject;
                    OnAssetToPool(loadPath, pool, loadedPrefab);     // use auto create pool in case pool was destroied when loaded
                    if(loaded != null)
                    {
                        loaded(loadedPrefab, pool);
                    }
                }, true);
            }
            else
            {
                if(loaded != null)
                {
                    loaded(prefab, pool);
                }
            }
        }

        /// <summary>
        /// Destroy a single spawned, fast API
        /// </summary>
        /// <param name="target"></param>
        /// <param name="loadPath"></param>
        /// <param name="poolName"></param>
        /// <param name="tryUnloadAsset"></param>
        /// <returns></returns>
        public bool DestroySpawned(GameObject target, string poolName = null, string loadPath = null, bool tryUnloadAsset = true)
        {
            if(target)
            {
                var pool = GetTargetSpawnedPool(target, poolName);
                if(pool)
                {
                    bool destoried = pool.DestroyOneSpawned(target, ref loadPath);
                    if(false == destoried)
                    {
                        Debug.LogWarning("DestroySpawned FAILED : " + target.name);
                    }
                    else if(tryUnloadAsset)
                    {
                        if(pool.HasSpawned(loadPath) == false)
                        {
                            DestroyTargetInPool(loadPath, pool.PoolName, tryUnloadAsset);
                        }
                    }
                    return destoried;
                }
            }
            return false;
        }

        /// <summary>
        /// destroy target instantiated gameobjects from a pool
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="poolName"></param>
        /// <returns></returns>
        public bool DestroyTargetInPool(string loadPath, string poolName = null, bool tryUnloadAsset = true)
        {
            var pool = GetPool(poolName, false);
            if(pool)
            {
                bool succ = pool.DestroyTargetSpawned(loadPath);
                if(succ)
                {
                    var list = _prefabBelongs.TryGetValue(loadPath);
                    if(list != null)
                    {
                        list.Remove(pool);
                    }
                    if(tryUnloadAsset)
                    {
                        if(list == null || list.Count == 0)
                        {
                            ResourcePool.UnloadAsset(loadPath, typeof(GameObject));
                        }
                    }
                }
                else if(tryUnloadAsset)
                {
                    var list = _prefabBelongs.TryGetValue(loadPath);
                    if(list == null || list.Count == 0)
                    {
                        ResourcePool.UnloadAsset(loadPath, typeof(GameObject));
                    }
                }
                return succ;
            }
            return false;
        }
        /// <summary>
        /// Destroy target pool, and if tryUnloadAsset is true it will determin to unload asset or not
        /// </summary>
        /// <param name="poolName"></param>
        public void DestroyPool(string poolName, bool tryUnloadAsset = true)
        {
            var pool = _pools.TryGetValue(poolName);
            if(pool)
            {
                if(tryUnloadAsset)
                {
                    var names = pool.GetObjectNames();
                    pool.DoDestroy();
                    _pools.Remove(poolName);
                    // must wait
                    foreach(var loadPath in names)
                    {
                        var belongPools = _prefabBelongs.TryGetValue(loadPath);
                        bool exists = false;
                        if(belongPools != null)
                        {
                            foreach(var tagPool in belongPools)
                            {
                                if(tagPool && tagPool.destroied == false && tagPool.Contains(loadPath))
                                {
                                    exists = true;
                                    break;
                                }
                            }
                        }
                        if(false == exists)
                        {
                            ResourcePool.UnloadAsset(loadPath, typeof(GameObject));
                        }
                    }
                }
                else
                {
                    pool.DoDestroy();
                    _pools.Remove(poolName);
                }
            }
        }
        /// <summary>
        /// Destroy All Pools
        /// </summary>
        public void DestroyAllPools(bool tryUnloadAsset = true)
        {
            if(tryUnloadAsset)
            {
                HashSet<string> allObjectNames = new HashSet<string>();
                foreach(var poolData in _pools)
                {
                    var names = poolData.Value.GetObjectNames();
                    if(names != null)
                    {
                        allObjectNames.UnionWith(names);
                    }
                    UnityObjectPoolManager.Instance.DestroyPool(poolData.Key);
                }

                if(allObjectNames.Count > 0)
                {
                    foreach(var loadPath in allObjectNames)
                    {
                        ResourcePool.UnloadAsset(loadPath, typeof(GameObject));
                    }
                }
            }
            else
            {
                foreach(var poolData in _pools)
                {
                    UnityObjectPoolManager.Instance.DestroyPool(poolData.Key);
                }
            }
            _pools.Clear();
            _prefabBelongs.Clear();
        }
        #endregion

        #region Help Funcs
        /// <summary>
        /// get target pool
        /// Notice: don't destroy pool directly, use DestroyPool call
        /// </summary>
        /// <param name="poolName"></param>
        /// <param name="createIfNoExists"></param>
        /// <returns></returns>
        private GameObjectPool GetPool(string poolName, bool createIfNoExists = true)
        {
            if(string.IsNullOrEmpty(poolName))
            {
                poolName = DefaultGameObjectPoolName;
            }
            var pool = _pools.TryGetValue(poolName);
            if(pool == false && createIfNoExists)
            {
                pool = UnityObjectPoolManager.Instance.GetObjectPool<GameObjectPool>(poolName, createIfNoExists);
                _pools[poolName] = pool;
            }
            return pool;
        }
        /// <summary>
        /// Get a spawn pool spawned target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="poolName"></param>
        /// <returns></returns>
        private GameObjectPool GetTargetSpawnedPool(GameObject target, string poolName)
        {
            int hashCode = target.GetHashCode();
            if(poolName == null)
            {
                poolName = DefaultGameObjectPoolName;
            }
            var pool = GetPool(poolName, false);    // check target or default first
            if(pool && pool.IsSpawnedFromThis(hashCode))
            {
                return pool;
            }
            else
            {
                foreach(var anyPool in _pools.Values)
                {
                    if(string.Equals(anyPool.PoolName, poolName, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    if(anyPool.IsSpawnedFromThis(hashCode))
                    {
                        return anyPool;
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// add asset to target pool and mark it, notice the prefab is an asset
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="pool"></param>
        /// <param name="prefab"></param>
        private void OnAssetToPool(string loadPath, GameObjectPool pool, GameObject prefab)
        {
            if(pool && pool.Add(loadPath, prefab))
            {
                _prefabBelongs.GetValue(loadPath, ()=> { return new HashSet<GameObjectPool>(); }).Add(pool);
            }
        }
        #endregion

    }
}

