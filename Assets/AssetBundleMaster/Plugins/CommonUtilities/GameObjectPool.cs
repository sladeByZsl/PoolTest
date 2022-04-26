using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AssetBundleMaster.ObjectPool
{
    using AssetBundleMaster.Common;
    using AssetBundleMaster.Extention;
    using AssetBundleMaster.ContainerUtilities;

    public class GameObjectPool : UnityObjectPool
    {
        #region Cache data defines
        public class SpawnCache
        {
            public readonly Dictionary<int, WeakReference<GameObject>> spawnedList = new Dictionary<int, WeakReference<GameObject>>();
            public readonly WeakList<GameObject> freeList = new WeakList<GameObject>();

            public GameObject GetFreeOne()
            {
                GameObject go = null;
                if(freeList.Count > 0)
                {
                    for(int i = freeList.Count - 1; i >= 0; i--)
                    {
                        go = freeList[i];
                        freeList.RemoveAt(i);
                        if(go)
                        {
                            return go;
                        }
                    }
                }
                return go;
            }

            public void Destroy(System.Action<GameObject> access = null)
            {
                foreach(var weak in spawnedList.Values)
                {
                    if(weak != null && weak.Object)
                    {
                        if(access != null)
                        {
                            access.Invoke(weak.Object);
                        }
                        UnityComponentExtention.Destroy(weak.Object);
                    }
                }
                foreach(var go in freeList)
                {
                    if(go)
                    {
                        if(access != null)
                        {
                            access.Invoke(go);
                        }
                        UnityComponentExtention.Destroy(go);
                    }
                }
            }

            public bool DestroyOne(GameObject target, ref int hashCode)
            {
                hashCode = target.GetHashCode();
                WeakReference<GameObject> cached;
                if(spawnedList.TryGetValue(hashCode, out cached) && cached != null && cached.Object == target)
                {
                    spawnedList.Remove(hashCode);
                    WeakReference<GameObject>.DeAllocate(cached);
                    UnityComponentExtention.Destroy(target);
                    return true;
                }
                return false;
            }

            public void AddSpawned(GameObject target)
            {
                spawnedList[target.GetHashCode()] = WeakReference<GameObject>.Allocate(target);
            }
            public void RemoveSpawned(GameObject target)
            {
                var hashCode = target.GetHashCode();
                WeakReference<GameObject> weak;
                spawnedList.TryGetValue(hashCode, out weak);
                spawnedList.Remove(hashCode);
                if(weak != null)
                {
                    WeakReference<GameObject>.DeAllocate(weak);
                }
            }
        }
        #endregion

        // dict to cache all spawned / free GameObjects [uniqueName, cache]
        private Dictionary<string, SpawnCache> m_spawnCaches = new Dictionary<string, SpawnCache>();
        // the inverse reference of Instantiated GameObject [hashCode, uniqueName]
        private Dictionary<int, string> m_spawnedInv = new Dictionary<int, string>();

        #region Main Funcs
        /// <summary>
        /// Add GameObject only
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public override bool Add(string uniqueName, Object gameObject)
        {
            if(gameObject && (gameObject is GameObject))
            {
                return base.Add(uniqueName, gameObject);
            }
            return false;
        }

        /// <summary>
        /// auto spawn a GameObject from pool or instantiate
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <param name="prefab"></param>
        /// <param name="linkToPool"> marks the instantiated gameObject is controlled by pool </param>
        /// <returns></returns>
        public GameObject Spawn(string uniqueName, GameObject prefab = null, bool active = true)
        {
            GameObject spawnTarget = null;
            var caches = m_spawnCaches.GetValue(uniqueName, ()=> { return new SpawnCache(); });
            spawnTarget = caches.GetFreeOne();
            bool newTarget = (spawnTarget == false);
            if(newTarget)
            {
                if(prefab == false)
                {
                    prefab = Get<GameObject>(uniqueName);
                }
                if(prefab)
                {
                    spawnTarget = GameObject.Instantiate(prefab);
                    spawnTarget.name = prefab.name;
                    spawnTarget.transform.SetParent(this.transform, true);
                    m_spawnedInv[spawnTarget.GetHashCode()] = uniqueName;   // mark this gameobject is spawn from this pool
                }
            }
            if(spawnTarget)
            {
                caches.AddSpawned(spawnTarget);
                spawnTarget.SetActive(active);
            }
            return spawnTarget;
        }

        /// <summary>
        /// Despawn an Instantiated GameObject to Pool
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public bool Despawn(GameObject go)
        {
            if(go)
            {
                var hashCode = go.GetHashCode();
                string uniqueName = null;
                // check this go is spawned from this pool?
                if(m_spawnedInv.TryGetValue(hashCode, out uniqueName))
                {
                    var cache = m_spawnCaches.GetValue(uniqueName, () => { return new SpawnCache(); });
                    cache.RemoveSpawned(go);                       // this may not linked, just try remove it
                    if(cache.freeList.Contains(go) == false)
                    {
                        cache.freeList.Add(go);
                        go.SetActive(false);
                        go.transform.SetParent(this.transform, true);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// destroy all spawned GameObjects by its unique name
        /// </summary>
        /// <param name="uniqueName"></param>
        public bool DestroyTargetSpawned(string uniqueName)
        {
            var cache = m_spawnCaches.TryGetValue(uniqueName);
            if(cache != null)
            {
                m_spawnCaches.Remove(uniqueName);
                cache.Destroy((_go) =>
                {
                    m_spawnedInv.Remove(_go.GetHashCode());
                });
            }
            return Remove(uniqueName);
        }

        /// <summary>
        /// Destroy target spawned, Slow API
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool DestroyOneSpawned(GameObject target, ref string uniqueName)
        {
            // search target
            if(string.IsNullOrEmpty(uniqueName) == false)
            {
                var cache = m_spawnCaches.TryGetValue(uniqueName);
                int hashCode = -1;
                if(cache != null && cache.DestroyOne(target, ref hashCode))
                {
                    m_spawnedInv.Remove(hashCode);
                    return true;
                }
            }
            // search all
            foreach(var kv in m_spawnCaches)
            {
                var cache = kv.Value;
                int hashCode = -1;
                if(cache.DestroyOne(target, ref hashCode))
                {
                    uniqueName = kv.Key;
                    m_spawnedInv.Remove(hashCode);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check target cache has any exists GameObjectS
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <returns></returns>
        public bool HasSpawned(string uniqueName)
        {
            var cache = m_spawnCaches.TryGetValue(uniqueName);
            if(cache != null && cache.spawnedList.Count > 0)
            {
                foreach(var weak in cache.spawnedList.Values)
                {
                    if(weak != null && weak.Object)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// check is spawned from this
        /// </summary>
        /// <param name="hashCode"></param>
        /// <returns></returns>
        public bool IsSpawnedFromThis(int hashCode)
        {
            return m_spawnedInv.ContainsKey(hashCode);
        }

        /// <summary>
        /// check is spawned from this
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public bool IsSpawnedFromThis(GameObject go)
        {
            return IsSpawnedFromThis(go.GetHashCode());
        }
        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach(var cache in m_spawnCaches.Values)
            {
                if(cache != null)
                {
                    cache.Destroy();
                }
            }
            m_spawnCaches.Clear();
            m_spawnedInv.Clear();
        }
    }
}

