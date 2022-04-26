using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AssetBundleMaster.Common
{
    using AssetBundleMaster.ObjectPool;

    /// <summary>
    /// The Customer WeakReference with reference type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WeakReference<T> : WeakReference where T : class
    {
        public T Object
        {
            get
            {
                return base.Target as T;
            }
            set
            {
                SetTarget(value);
            }
        }
        public int hash = int.MinValue;

        static WeakReference()
        {
            GlobalAllocator<WeakReference<T>>.Set(() => { return new WeakReference<T>(); }, (_weak) => { _weak.Clear(); });
            GlobalArrayAllocator<WeakReference<T>>.Set((_array) =>
            {
                foreach(var data in _array)
                {
                    if(data != null)
                    {
                        GlobalAllocator<WeakReference<T>>.DeAllocate(data);
                    }
                }
                Array.Clear(_array, 0, _array.Length);
            });
        }

        public WeakReference() : base(null) { }
        public WeakReference(T target) : base(null) { SetTarget(target); }

        public WeakReference<T> SetTarget(T target)
        {
            hash = (target != null ? target.GetHashCode() : int.MinValue);
            base.Target = target;
            return this;
        }

        public void Clear()
        {
            SetTarget(null);
        }

        public bool EqualsTo(int hashCode)
        {
            return hash == hashCode && Target != null;
        }

        // pool funcs
        public static WeakReference<T> Allocate()
        {
            return GlobalAllocator<WeakReference<T>>.Allocate().SetTarget(null);
        }
        public static WeakReference<T> Allocate(T ins)
        {
            return GlobalAllocator<WeakReference<T>>.Allocate().SetTarget(ins);
        }
        public static void DeAllocate(WeakReference<T> ins)
        {
            if (ins != null)
            {
                ins.SetTarget(null);
                GlobalAllocator<WeakReference<T>>.DeAllocate(ins);
            }           
        }
        public static void DeAllocate(ref WeakReference<T> ins)
        {
            if (ins != null)
            {
                ins.SetTarget(null);
                GlobalAllocator<WeakReference<T>>.DeAllocate(ins);
                ins = null;
            }
        }

        public static WeakReference<T>[] AllocateArray(int size, bool forcePowerof2 = false)
        {
            return GlobalArrayAllocator<WeakReference<T>>.Allocate(size, forcePowerof2);
        }
        public static void DeAllocateArray(ref WeakReference<T>[] data, bool forcePowerof2 = false)
        {
            GlobalArrayAllocator<WeakReference<T>>.DeAllocate(data, forcePowerof2);
            data = null;
        }
    }
}