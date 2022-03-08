using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace zsl.Pool
{
    public class PoolConfig
    {
        public string poolName = "";//****必填项，对象池的名称
        
        //预加载的数量
        public int preloadAmount;
        //预加载是否开启分帧,>0表示开启分帧，同时每帧实例化的数量
        public int preloadFrame;

        public int limitAmount = 0;//如果大于0表示限制加载，同时最多的Spawn的数量为limitAmount,超过则不加载;=0表示没有限制

        public bool autoDestroy = false;//是否自动销毁，开启则自动销毁；否则池子里的GameObject不会自动销毁
        public int keepMax = 10;//如果开启自动销毁，当spawn+despawn的总数超过keepMax，触发销毁
        public int perDestroyNum = 3;//触发销毁时，每帧最多销毁几个
        
        
    }

    public class PoolBase
    {
        public virtual void Clear()
        {
        }
    }
    
    
    public class Pool<T>:PoolBase  where T : class, new()
    {
        private Func<T> mCreateFunc = null;
        private Action<T> mResetFunc = null;
        private PoolConfig mPoolConfig = null;
        private Queue<T> mCache = new Queue<T>();
        public Pool(Func<T> createFunc, Action<T> resetFunc, PoolConfig poolConfig)
        {
            mCreateFunc = createFunc;
            mResetFunc = resetFunc;
            mPoolConfig = poolConfig;
        }

        public T Get()
        {
            T t = default(T);
            lock (mCache)
            {
                if (mCache.Count > 0)
                {
                    t = mCache.Dequeue();
                }
            }
            if (t == null)
            {
                if (mCreateFunc != null)
                {
                    t = mCreateFunc();
                }
                else
                {
                    t = new T();
                }
            }
            return t;
        }
        
        public void Recycle(T t)
        {
            if (t == null)
            {
                return;
            }
            lock (mCache)
            {
                if (mResetFunc != null)
                {
                    mResetFunc.Invoke(t);
                }
                mCache.Enqueue(t);
            }
        }

        public override void Clear()
        {
            lock (mCache)
            {
                mCache.Clear();
            }
        }
    }
}
