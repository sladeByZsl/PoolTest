using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace AssetBundleMaster.ObjectPool
{
    using AssetBundleMaster.Extention;

    #region Generic Type Allocator
    /// <summary>
    /// base class of Class allocate
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CommonAllocator<T> where T : class, new()
    {
        protected readonly Queue<T> _freeList = new Queue<T>();
        private System.Func<T> _create = null;
        private System.Action<T> _deallocate = null;

        public CommonAllocator() { }

        public CommonAllocator<T> Set(System.Func<T> create, System.Action<T> deAllocate = null)
        {
            _create = create;
            _deallocate = deAllocate;
            return this;
        }

        #region Main Funcs
        // allocate a target
        public virtual T Allocate()
        {
            T retVal = (_freeList.Count > 0) ? _freeList.Dequeue() : (_create != null ? _create.Invoke() : new T());
            return retVal;
        }
        // recycle the target to pool
        public virtual void DeAllocate(T ins)
        {
            if(ins != null)
            {
                if(_deallocate != null)
                {
                    _deallocate.Invoke(ins);
                }
                _freeList.Enqueue(ins);         // the Contains is also a iterator that will create new enumerator
            }
        }
        // clear all free list items
        public virtual void Clear()
        {
            _freeList.Clear();
        }
        // the selectable clear free pool
        protected virtual void Trim(int leftCount = 0)
        {
            _freeList.RemoveFrom(leftCount);
        }
        #endregion
    }

    public static class GlobalAllocator<T> where T : class, new()
    {
        private static readonly CommonAllocator<T> _instance = new CommonAllocator<T>();

        public static T Allocate()
        {
            return _instance.Allocate();
        }

        public static void DeAllocate(T ins)
        {
            _instance.DeAllocate(ins);
        }

        public static void Clear()
        {
            _instance.Clear();
        }

        public static void Set(System.Func<T> create, System.Action<T> deAllocate = null)
        {
            _instance.Set(create, deAllocate);
        }
    }
    #endregion

    #region Array Allocator
    /// <summary>
    /// this byte allocate get / create T[] array from pool that allocated by power of 2, with no thread safe
    /// </summary>
    public class ArrayAllocator<T>
    {
        protected readonly Dictionary<int, Queue<T[]>> _freeList = new Dictionary<int, Queue<T[]>>();     // free dict
        private System.Action<T[]> _deallocate = null;

        protected const int _maxHoldingSize = 2048;           // the left max size of auto trim
        protected const int _forceDisposeSize = 65535;       // the force release size of the array size -- avoid too large size in the pool

        #region Main Funcs
        /// <summary>
        /// Get tag size byte buffer from pool
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public virtual T[] Allocate(int size, bool forcePowerof2 = true)
        {
            T[] freeBuffer = GetArray(forcePowerof2 ? NextPowerOfTwo(size) : size);
            return freeBuffer;
        }
        /// <summary>
        /// turn back the buffer to free pool, but it must be a powered length / or adjust it to a lower grade -- The previous power of 2 
        /// </summary>
        /// <param name="data"></param>
        public virtual void DeAllocate(T[] data, bool forcePowerof2 = true)
        {
            if(data == null)
            {
                return;
            }
            if(_deallocate != null)
            {
                _deallocate.Invoke(data);
            }
            Array.Clear(data, 0, data.Length);
            int size = forcePowerof2 ? FloorPowerOfTwo(data.Length) : data.Length;
            var freeList = _freeList.GetValue(size, ()=> { return new Queue<T[]>(); });
            freeList.Enqueue(data);
        }
        /// <summary>
        /// clear free pool that too large size
        /// </summary>
        public virtual void Trim()
        {
            ReleaseFreeListWithAliveNum(_maxHoldingSize);
        }
        /// <summary>
        /// clrear all free buffers
        /// </summary>
        public virtual void ReleaseAllFreeBuffer()
        {
            ReleaseFreeListWithAliveNum(0);
        }

        public ArrayAllocator<T> Set(System.Action<T[]> deAllocate)
        {
            _deallocate = deAllocate;
            return this;
        }
        #endregion

        #region Help Funcs -- action
        // wrapped fnuc for pop node from free dict
        protected T[] GetArray(int size)
        {
            if(size >= 0)
            {
                var queue = _freeList.TryGetValue(size);
                return (queue != null && queue.Count > 0) ? queue.Dequeue() : new T[size];  // buffer allocated
            }
            return null;
        }
        // dispose free list with count
        protected virtual void ReleaseFreeListWithAliveNum(int num)
        {
            foreach(var pair in _freeList)
            {
                var queue = pair.Value;
                if(pair.Key > _forceDisposeSize)
                {
                    num = 0;
                }
                if(queue != null && queue.Count > num)
                {
                    queue.RemoveFrom(num);
                }
            }
        }
        // just allocate
        public static T[] ForceAllocate(int size, bool forcePowerof2)
        {
            int fixedSize = forcePowerof2 ? NextPowerOfTwo(size) : size;
            return new T[fixedSize];
        }

        /// <summary>
        /// Check the number is power of 2, Minimal Faster then Mathf
        /// </summary>
        /// <param name="value"> the number </param>
        /// <returns></returns>
        public static bool IsPowerOfTwo(int value)
        {
            return (value > 0) && ((value & (value - 1)) == 0);
        }

        // wrapped func get val power of 2
        public static int NextPowerOfTwo(int size)
        {
            return (size <= 2) ? 2 : UnityEngine.Mathf.NextPowerOfTwo(size);
        }

        /// <summary>
        /// Floor to power of 2
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int FloorPowerOfTwo(int value)
        {
            if(IsPowerOfTwo(value))
            {
                return value;
            }
            return UnityEngine.Mathf.NextPowerOfTwo(value) >> 1;
        }
        #endregion
    }

    /// <summary>
    /// this byte allocate get / create T[] array from pool that allocated by power of 2, thread safe
    /// no more ref to the used list for avoid leak
    /// </summary>
    public static class GlobalArrayAllocator<T>
    {
        private static readonly ArrayAllocator<T> _instance = new ArrayAllocator<T>();

        #region Main Funcs
        /// <summary>
        /// Get tag size byte buffer from pool
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static T[] Allocate(int size, bool forcePowerof2 = true)
        {
            return _instance.Allocate(size, forcePowerof2);
        }
        /// <summary>
        /// turn back the buffer to free pool, but it must be a powered length
        /// </summary>
        /// <param name="data"></param>
        public static void DeAllocate(T[] data, bool forcePowerof2 = true)
        {
            _instance.DeAllocate(data, forcePowerof2);
        }
        /// <summary>
        /// clear free pool that too large size
        /// </summary>
        public static void Trim()
        {
            _instance.Trim();
        }
        /// <summary>
        /// clrear all free buffers
        /// </summary>
        public static void ReleaseAllFreeBuffer()
        {
            _instance.ReleaseAllFreeBuffer();
        }

        public static void Set(System.Action<T[]> deAllocate)
        {
            _instance.Set(deAllocate);
        }
        #endregion
    }
    #endregion
}
