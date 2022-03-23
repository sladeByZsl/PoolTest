using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ELEX.NewPool
{
    public partial class PoolGroup 
    {
        #region Private Properties
        private List<PrefabPool> _prefabPools = new List<PrefabPool>();
        internal List<Transform> _spawned = new List<Transform>();
        private Dictionary<string, Transform> prefabs = new Dictionary<string, Transform>();
        #endregion Private Properties

        #region Constructor and Init
        public void CreatePool(PrefabPool prefabPool)
        {
            bool isAlreadyPool = this.GetPool(prefabPool.prefabGO) != null;
            if (!isAlreadyPool)
            {
                prefabPool.poolGroup = this;
                this._prefabPools.Add(prefabPool);
                this.prefabs.Add(prefabPool.prefabGO.name, prefabPool.prefab);
            }
            prefabPool.inspectorInstanceConstructor();
        }
        private void OnDestruct_PrefabPool()
        {
            foreach (PrefabPool pool in this._prefabPools) pool.SelfDestruct();
            this._prefabPools.Clear();
            this._spawned.Clear();
        }
        #endregion Constructor and Init

        #region Pool Functionality
        public Transform Spawn(Transform prefab, Vector3 pos, Quaternion rot, Transform parent)
        {
            Transform inst;

            #region Use from Pool
            for (int i = 0; i < this._prefabPools.Count; i++)
            {
                if (this._prefabPools[i].prefabGO == prefab.gameObject)
                {
                    PrefabPool prefabPool = this._prefabPools[i];
                    inst = prefabPool.SpawnInstance(pos, rot);
                    if (inst == null) return null;

                    if (parent != null) 
                    {
                        inst.parent = parent;
                    }
                    else if (inst.parent != prefabPool.parent)  
                    {
                        inst.parent = prefabPool.parent;
                    }
                    this._spawned.Add(inst);
                    return inst;
                }
            }
            #endregion Use from Pool

            #region New PrefabPool
            PrefabPool newPrefabPool = new PrefabPool();
            newPrefabPool.prefab = prefab;
            this.CreatePool(newPrefabPool);
            inst = newPrefabPool.SpawnInstance(pos, rot);
            if (parent != null)
            {
                inst.parent = parent;
            }
            else
            {
                inst.parent = newPrefabPool.parent;  
            }
            this._spawned.Add(inst);
            #endregion New PrefabPool
            return inst;
        }
        public Transform Spawn(Transform prefab, Vector3 pos, Quaternion rot)
        {
            Transform inst = this.Spawn(prefab, pos, rot, null);
            if (inst == null) return null;

            return inst;
        }
        public Transform Spawn(Transform prefab)
        {
            return this.Spawn(prefab, Vector3.zero, Quaternion.identity);
        }
        public Transform Spawn(Transform prefab, Transform parent)
        {
            return this.Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
        }
        public Transform Spawn(string prefabName)
        {
            Transform prefab = this.prefabs[prefabName];
            return this.Spawn(prefab);
        }
        public Transform Spawn(string prefabName, Transform parent)
        {
            Transform prefab = this.prefabs[prefabName];
            return this.Spawn(prefab, parent);
        }
        public Transform Spawn(string prefabName, Vector3 pos, Quaternion rot)
        {
            Transform prefab = this.prefabs[prefabName];
            return this.Spawn(prefab, pos, rot);
        }
        public Transform Spawn(string prefabName, Vector3 pos, Quaternion rot, 
            Transform parent)
        {
            Transform prefab = this.prefabs[prefabName];
            return this.Spawn(prefab, pos, rot, parent);
        }
        public void Despawn(Transform instance)
        {
            bool despawned = false;
            for (int i = 0; i < this._prefabPools.Count; i++)
            {
                if (this._prefabPools[i]._spawned.Contains(instance))
                {
                    despawned = this._prefabPools[i].DespawnInstance(instance);
                    break;
                }
                else if (this._prefabPools[i]._despawned.Contains(instance))
                {
                    Debug.LogError(
                        string.Format("SpawnPool {0}: {1} has already been despawned. " +
                            "You cannot despawn something more than once!",
                            this.groupName,
                            instance.name));
                    return;
                }
            }
            if (!despawned)
            {
                Debug.LogError(string.Format("SpawnPool {0}: {1} not found in SpawnPool",
                    this.groupName,
                    instance.name));
                return;
            }
            this._spawned.Remove(instance);
        }
        public void Despawn(Transform instance, Transform parent)
        {
            instance.parent = parent;
            this.Despawn(instance);
        }
        public void DespawnAll()
        {
            var spawned = new List<Transform>(this._spawned);
            for (int i = 0; i < spawned.Count; i++)
                this.Despawn(spawned[i]);
        }

        public void ClearAllDespawnPrefab()
        {
            
        }
        
        #endregion Pool Functionality

        #region Utility Functions
        public PrefabPool GetPool(GameObject prefab)
        {
            for (int i = 0; i < this._prefabPools.Count; i++)
            {
                if (this._prefabPools[i].prefabGO == null)
                    Debug.LogError(string.Format("SpawnPool {0}: PrefabPool.prefabGO is null",
                        this.groupName));

                if (this._prefabPools[i].prefabGO == prefab)
                    return this._prefabPools[i];
            }
            return null;
        }
        #endregion Utility Functions

    }
}
