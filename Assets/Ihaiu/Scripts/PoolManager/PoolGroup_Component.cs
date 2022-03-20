using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Ihaius
{
    public partial class PoolGroup 
    {
        private Dictionary<Type, ComponentPool<MonoBehaviour>> compPrefabs = new Dictionary<Type, ComponentPool<MonoBehaviour>>();
        public void CreatePool<T>(ComponentPool<T> componentPool) where T : MonoBehaviour
        {
            bool isAlreadyPool = this.GetPoolType(typeof(T)) != null;
            if (!isAlreadyPool)
            {
                componentPool.poolGroup = this;
                this.compPrefabs.Add(typeof(T), componentPool as ComponentPool<MonoBehaviour>);
            }

            componentPool.inspectorInstanceConstructor();
            if (componentPool.preloaded)
            {
                if (this.logMessages)
                    Debug.Log(string.Format("PoolGroup {0}: 预实例化对象中 preloadAmount={1} {2}",
                        this.groupName,
                        componentPool.preloadAmount,
                        componentPool.prefabGO.name));

                componentPool.PreloadInstances();
            }
        }
        
        public ComponentPool<MonoBehaviour> GetPoolType(Type types)
        {
            ComponentPool<MonoBehaviour> pool;
            if (compPrefabs.TryGetValue(types,out pool))
            {
                return pool;
            }
            // Nothing found
            return null;
        }
        
        public T SpawnMonoBehaviour<T>(params object[] args) where T : MonoBehaviour
        {
            Type type =  typeof(T);

            ComponentPool<MonoBehaviour> pool = GetPoolType(type);

            if (pool == null)
            {
                pool = new ComponentPool<MonoBehaviour>();
                CreatePool(pool);
            }

            var inst = pool.SpawnInstance(args);

            if (inst == null)
                return default(T);

            pool.ItemOnSpawned(inst);

            return inst as T;
        }
    }
}
