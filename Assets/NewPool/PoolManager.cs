using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace zsl.Pool
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        //多个Pool
        Dictionary<string, PoolBase> mDicPool = new Dictionary<string, PoolBase>();

        void Awake()
        {
            Instance = this;
                
            DontDestroyOnLoad(this);
        }

        void Update()
        {
        }

        public Pool<T> GetOrCreatePool<T>(Func<T> createFun, Action<T> resetFunc, PoolConfig poolConfig) where T : class, new()
        {
            Pool<T> pool = null;
            lock (mDicPool)
            {
                if (!mDicPool.ContainsKey(poolConfig.poolName))
                {
                    pool = new Pool<T>(createFun, resetFunc, poolConfig);
                    mDicPool[poolConfig.poolName] = pool;
                }
                else
                {
                    pool = mDicPool[poolConfig.poolName] as Pool<T>;
                }
            }
            return pool;
        }
    }

}