using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.ObjectPool
{
    using AssetBundleMaster.GameUtilities;
    using AssetBundleMaster.Extention;

    /// <summary>
    /// UnityObjectPool Manager
    /// </summary>
    public sealed class UnityObjectPoolManager : SingletonComponent<UnityObjectPoolManager>
    {
        public const string DefaultPoolName = "[DefaultPool]";

        // cached pools, with diferent pool name
        private Dictionary<string, UnityObjectPool> _pools = new Dictionary<string, UnityObjectPool>();

        #region Main Funcs
        /// <summary>
        /// Get / Create Target Pool
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="poolName"></param>
        /// <param name="createIfNoExists"></param>
        /// <returns></returns>
        public T GetObjectPool<T>(string poolName = null, bool createIfNoExists = true) where T : UnityObjectPool
        {
            if(string.IsNullOrEmpty(poolName))
            {
                poolName = DefaultPoolName;
            }
            var objectPool = _pools.TryGetValue(poolName) as T;     // the type may not the same
            if(objectPool == false && createIfNoExists)
            {
                var poolObject = new GameObject(poolName);
                poolObject.transform.SetParent(this.transform);
                objectPool = poolObject.AddComponent<T>();
                objectPool.PoolName = poolName;
                _pools[poolName] = objectPool;
            }
            return objectPool;
        }

        /// <summary>
        /// Delete Target Pool
        /// </summary>
        /// <param name="poolName"></param>
        public void DestroyPool(string poolName)
        {
            var pool = GetObjectPool<UnityObjectPool>(poolName, false);
            if(pool)
            {
                pool.DoDestroy();
                _pools.Remove(poolName);
            }
        }
        #endregion

    }
}

