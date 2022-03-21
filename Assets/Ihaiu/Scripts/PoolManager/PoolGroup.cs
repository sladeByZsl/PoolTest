using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ELEX.NewPool
{
    public partial class PoolGroup 
    {
        /** 组名称 */
        public string groupName;
        /** 是否打印日志信息 */
        public bool logMessages = true;

        internal Dictionary<Type, AbstractObjectPool> typePools = new Dictionary<Type, AbstractObjectPool>();

        public PoolGroup(string groupName)
        {
            this.groupName = groupName;
        }

        public void OnStart()
        {
            if (this.logMessages)
                Debug.Log(string.Format("PoolGroup {0}: Initializing..", this.groupName));

            PoolManager.Instance.Add(this);
        }

        public void OnUpdate()
        {
            foreach(var item in typePools)
            {
                item.Value.OnUpdate();
            }

            foreach (var item in _prefabPools)
            {
                item.OnUpdate();
            }
        }
        
        internal void OnDestruct()
        {
            if (this.logMessages)
                Debug.Log(string.Format("PoolGroup {0}: Destroying...", this.groupName));

            PoolManager.Instance.Remove(this);
            foreach(var kvp in typePools)
            {
                kvp.Value.SelfDestruct();
            }
            typePools.Clear();
            OnDestruct_PrefabPool();
        }


        /** 添加一个对象池 */
        public void CreatePool<T>(ObjectPool<T> objectPool)
        {
            Type type = typeof(T);

            bool isAlreadyPool = this.GetPool(type) == null ? false : true;
            if (!isAlreadyPool)
            {
                objectPool.poolGroup = this;
                typePools.Add(type, objectPool);
            }

            objectPool.inspectorInstanceConstructor();
        }
        
        /** 获取对应类型的Pool */
        public ObjectPool<T> GetPool<T>()
        {
            return (ObjectPool<T>) GetPool(typeof(T));
        }

        /** 获取对应类型的Pool */
        public AbstractObjectPool GetPool(Type type)
        {

            if (typePools.ContainsKey(type))
            {
                return typePools[type];
            }
            return null;
        }

        /** 获取一个对应类型的实例对象 */
        public T Spawn<T>(params object[] args)
        {
            T inst;

            ObjectPool<T> pool = GetPool<T>();

            if (pool == null)
            {
                pool = new ObjectPool<T>();
                CreatePool<T>(pool);
            }

            inst = pool.SpawnInstance(args);

            // This only happens if the limit option was used for this
            //   Prefab Pool.
            if (inst == null)
                return default(T);
            return inst;
        }

        /** 将实例对象设置为闲置状态 */
        public void Despawn<T>(T inst)
        {
            ObjectPool<T> pool = GetPool<T>();
            if (pool != null)
            {
                pool.DespawnInstance(inst);
            }
        }

        public void ClearPool<T>()
        {
            ObjectPool<T> pool = GetPool<T>();
            if (pool != null)
            {
                typePools.Remove(typeof(T));
                pool.SelfDestruct();
            }
        }
    }
}
