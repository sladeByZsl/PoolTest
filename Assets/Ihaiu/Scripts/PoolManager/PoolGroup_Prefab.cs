using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Ihaius
{
    public partial class PoolGroup 
    {

        /** 检测粒子设为闲置状态最长时间 */
        public float maxParticleDespawnTime = 300;

        #region Private Properties
        private List<PrefabPool> _prefabPools = new List<PrefabPool>();
        internal List<Transform> _spawned = new List<Transform>();
        private Dictionary<string, Transform> prefabs = new Dictionary<string, Transform>();
        #endregion Private Properties



        #region Constructor and Init

        private void OnDestruct_Prefab()
        {

            foreach (PrefabPool pool in this._prefabPools) pool.SelfDestruct();

            this._prefabPools.Clear();
            this._spawned.Clear();
        }


       
        public void CreatePool(PrefabPool prefabPool)
        {
            bool isAlreadyPool = this.GetPool(prefabPool.prefabGO) == null ? false : true;
            if (!isAlreadyPool)
            {
                prefabPool.poolGroup = this;
                this._prefabPools.Add(prefabPool);
                this.prefabs.Add(prefabPool.prefabGO.name, prefabPool.prefab);
            }

            prefabPool.inspectorInstanceConstructor();
            if (prefabPool.preloaded)
            {
                if (this.logMessages)
                    Debug.Log(string.Format("PoolGroup {0}: 预实例化对象中 preloadAmount={1} {2}",
                        this.groupName,
                        prefabPool.preloadAmount,
                        prefabPool.prefabGO.name));

                prefabPool.PreloadInstances();
            }
        }


     
        public void Add(Transform instance, string prefabName, bool despawn, bool parent)
        {
            for (int i = 0; i < this._prefabPools.Count; i++)
            {
                if (this._prefabPools[i].prefabGO == null)
                {
                    Debug.LogError("Unexpected Error: PrefabPool.prefab is null");
                    return;
                }

                if (this._prefabPools[i].prefabGO.name == prefabName)
                {
                    PrefabPool prefabPool = this._prefabPools[i];
                    prefabPool.AddUnpooled(instance, despawn);

                    if (this.logMessages)
                        Debug.Log(string.Format(
                            "PoolGroup {0}: Adding previously unpooled instance {1}",
                            this.groupName,
                            instance.name));

                    if (parent) instance.parent = prefabPool.parent;

                    // New instances are active and must be added to the internal list 
                    if (!despawn) this._spawned.Add(instance);

                    return;
                }
            }

            // Log an error if a PrefabPool with the given name was not found
            Debug.LogError(string.Format("PoolGroup {0}: PrefabPool {1} not found.",
                this.groupName,
                prefabName));

        }
        #endregion Constructor and Init






        #region Pool Functionality
        public Transform Spawn(Transform prefab, Vector3 pos, Quaternion rot, Transform parent)
        {
            Transform inst;

            #region Use from Pool
            for (int i = 0; i < this._prefabPools.Count; i++)
            {
                // Determine if the prefab was ever used as explained in the docs
                //   I believe a comparison of two references is processor-cheap.
                if (this._prefabPools[i].prefabGO == prefab.gameObject)
                {
                    PrefabPool prefabPool = this._prefabPools[i];
                    inst = prefabPool.SpawnInstance(pos, rot);

                    // This only happens if the limit option was used for this
                    //   Prefab Pool.
                    if (inst == null) return null;

                    if (parent != null)  // User explicitly provided a parent
                    {
                        inst.parent = parent;
                    }
                    else if (inst.parent != prefabPool.parent)  // Auto organize?
                    {
                        // If a new instance was created, it won't be grouped
                        inst.parent = prefabPool.parent;
                    }

                  
                    this._spawned.Add(inst);

                    prefabPool.ItemOnSpawned(inst);

                    return inst;
                }
            }
            #endregion Use from Pool


            #region New PrefabPool
            // The prefab wasn't found in any PrefabPools above. Make a new one
            PrefabPool newPrefabPool = new PrefabPool();
            newPrefabPool.prefab = prefab;
            this.CreatePool(newPrefabPool);

            // Spawn the new instance (Note: prefab already set in PrefabPool)
            inst = newPrefabPool.SpawnInstance(pos, rot);

            if (parent != null)  // User explicitly provided a parent
            {
                inst.parent = parent;
            }
            else  // Auto organize
            {
                inst.parent = newPrefabPool.parent;  
            }


            // New instances are active and must be added to the internal list 
            this._spawned.Add(inst);
            #endregion New PrefabPool

            newPrefabPool.ItemOnSpawned(inst);

            // Done!
            return inst;
        }


        /// <summary>
        /// See primary Spawn method for documentation.
        /// </summary>
        public Transform Spawn(Transform prefab, Vector3 pos, Quaternion rot)
        {
            Transform inst = this.Spawn(prefab, pos, rot, null);

            // Can happen if limit was used
            if (inst == null) return null;

            return inst;
        }


        /// <summary>
        /// See primary Spawn method for documentation.
        /// 
        /// Overload to take only a prefab and instance using an 'empty' 
        /// position and rotation.
        /// </summary>
        public Transform Spawn(Transform prefab)
        {
            return this.Spawn(prefab, Vector3.zero, Quaternion.identity);
        }


        /// <summary>
        /// See primary Spawn method for documentation.
        /// 
        /// Convienince overload to take only a prefab  and parent the new 
        /// instance under the given parent
        /// </summary>
        public Transform Spawn(Transform prefab, Transform parent)
        {
            return this.Spawn(prefab, Vector3.zero, Quaternion.identity, parent);
        }


        /// <summary>
        /// See primary Spawn method for documentation.
        /// 
        /// Overload to take only a prefab name. The cached reference is pulled  
        /// from the SpawnPool.prefabs dictionary.
        /// </summary>
        public Transform Spawn(string prefabName)
        {
            Transform prefab = this.prefabs[prefabName];
            return this.Spawn(prefab);
        }


        /// <summary>
        /// See primary Spawn method for documentation.
        /// 
        /// Convienince overload to take only a prefab name and parent the new 
        /// instance under the given parent
        /// </summary>
        public Transform Spawn(string prefabName, Transform parent)
        {
            Transform prefab = this.prefabs[prefabName];
            return this.Spawn(prefab, parent);
        }


        /// <summary>
        /// See primary Spawn method for documentation.
        /// 
        /// Overload to take only a prefab name. The cached reference is pulled from 
        /// the SpawnPool.prefabs dictionary. An instance will be set to the passed 
        /// position and rotation.
        /// </summary>
        public Transform Spawn(string prefabName, Vector3 pos, Quaternion rot)
        {
            Transform prefab = this.prefabs[prefabName];
            return this.Spawn(prefab, pos, rot);
        }


        /// <summary>
        /// See primary Spawn method for documentation.
        /// 
        /// Convienince overload to take only a prefab name and parent the new 
        /// instance under the given parent. An instance will be set to the passed 
        /// position and rotation.
        /// </summary>
        public Transform Spawn(string prefabName, Vector3 pos, Quaternion rot, 
            Transform parent)
        {
            Transform prefab = this.prefabs[prefabName];
            return this.Spawn(prefab, pos, rot, parent);
        }
        

        /// <summary>
        /// If the passed object is managed by the SpawnPool, it will be 
        /// deactivated and made available to be spawned again.
        /// Despawned instances are removed from the primary list.
        /// </summary>
        /// <param name="item">The transform of the gameobject to process</param>
        public void Despawn(Transform instance)
        {
            // Find the item and despawn it
            bool despawned = false;
            for (int i = 0; i < this._prefabPools.Count; i++)
            {
                if (this._prefabPools[i]._spawned.Contains(instance))
                {
                    despawned = this._prefabPools[i].DespawnInstance(instance);
                    break;
                }  // Protection - Already despawned?
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

            // If still false, then the instance wasn't found anywhere in the pool
            if (!despawned)
            {
                Debug.LogError(string.Format("SpawnPool {0}: {1} not found in SpawnPool",
                    this.groupName,
                    instance.name));
                return;
            }

            // Remove from the internal list. Only active instances are kept. 
            //   This isn't needed for Pool functionality. It is just done 
            //   as a user-friendly feature which has been needed before.
            this._spawned.Remove(instance);
        }


        /// <summary>
        /// See docs for Despawn(Transform instance) for basic functionalty information.
        /// Convienince overload to provide the option to re-parent for the instance 
        /// just before despawn.
        /// </summary>
        public void Despawn(Transform instance, Transform parent)
        {
            instance.parent = parent;
            this.Despawn(instance);
        }


        /// <description>
        /// See docs for Despawn(Transform instance). This expands that functionality.
        ///   If the passed object is managed by this SpawnPool, it will be 
        ///   deactivated and made available to be spawned again.
        /// </description>
        /// <param name="item">The transform of the instance to process</param>
        /// <param name="seconds">The time in seconds to wait before despawning</param>
        public void Despawn(Transform instance, float seconds)
        {
            this.StartCoroutine(this.DoDespawnAfterSeconds(instance, seconds, false, null));
        }


        /// <summary>
        /// See docs for Despawn(Transform instance) for basic functionalty information.
        ///     
        /// Convienince overload to provide the option to re-parent for the instance 
        /// just before despawn.
        /// </summary>
        public void Despawn(Transform instance, float seconds, Transform parent)
        {
            this.StartCoroutine(this.DoDespawnAfterSeconds(instance, seconds, true, parent));
        }


        /// <summary>
        /// Waits X seconds before despawning. See the docs for DespawnAfterSeconds()
        /// the argument useParent is used because a null parent is valid in Unity. It will 
        /// make the scene root the parent
        /// </summary>
        private IEnumerator DoDespawnAfterSeconds(Transform instance, float seconds, bool useParent, Transform parent)
        {
            GameObject go = instance.gameObject;
            while (seconds > 0)
            {
                yield return null;

                // If the instance was deactivated while waiting here, just quit
                if (!go.activeInHierarchy)
                    yield break;

                seconds -= Time.deltaTime;
            }

            if (useParent)
                this.Despawn(instance, parent);
            else
                this.Despawn(instance);
        }


        /// <description>
        /// Despawns all active instances in this SpawnPool
        /// </description>
        public void DespawnAll()
        {
            var spawned = new List<Transform>(this._spawned);
            for (int i = 0; i < spawned.Count; i++)
                this.Despawn(spawned[i]);
        }


        /// <description>
        /// Returns true if the passed transform is currently spawned.
        /// </description>
        /// <param name="item">The transform of the gameobject to test</param>
        public bool IsSpawned(Transform instance)
        {
            return this._spawned.Contains(instance);
        }

        #endregion Pool Functionality



        #region Utility Functions

        /// <summary>
        /// Returns the prefab pool for a given prefab.
        /// </summary>
        /// <param name="prefab">The GameObject of an instance</param>
        /// <returns>PrefabPool</returns>
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

            // Nothing found
            return null;
        }


        /// <summary>
        /// Returns the prefab used to create the passed instance. 
        /// This is provided for convienince as Unity doesn't offer this feature.
        /// </summary>
        /// <param name="instance">The GameObject of an instance</param>
        /// <returns>GameObject</returns>
        public GameObject GetPrefab(GameObject instance)
        {
            for (int i = 0; i < this._prefabPools.Count; i++)
                if (this._prefabPools[i].Contains(instance.transform))
                    return this._prefabPools[i].prefabGO;

            // Nothing found
            return null;
        }

        #endregion Utility Functions

    }
}
