using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AssetBundleMaster.ContainerUtilities
{
    using AssetBundleMaster.Common;
    using AssetBundleMaster.ObjectPool;

    /// <summary>
    /// Wrapped Weak List, logic is base on the hash compare
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WeakList<T> : IEnumerator<T>, IEnumerable, IEnumerable<T>, IList<T>, IDisposable where T : class
    {
        private List<WeakReference<T>> _refList = new List<WeakReference<T>>();

        public T this[int index]
        {
            get
            {
                if(index >= 0 && index < _refList.Count)
                {
                    var tag = _refList[index];
                    if(tag != null)
                    {
                        return tag.Object;
                    }
                }
                return null;
            }
            set
            {
                Replace(index, value);
            }
        }
        public int Count
        {
            get
            {
                return _refList.Count;
            }
        }

        // controls
        private int _index = -1;
        public T Current
        {
            get
            {
                var tag = _refList[_index];
                if(tag != null)
                {
                    return tag.Object;
                }
                return null;
            }
        }
        public bool IsReadOnly
        {
            get { return false; }
        }

        public WeakList() { }
        public WeakList(IEnumerable<T> collection)
        {
            this.AddRange(collection);
        }

        static WeakList()
        {
            GlobalAllocator<WeakList<T>>.Set(() => { return new WeakList<T>(); }, (_weak) => { _weak.Clear(); });
        }

        public static WeakList<T> Allocate()
        {
            return GlobalAllocator<WeakList<T>>.Allocate();
        }
        public static void DeAllocate(WeakList<T> ins)
        {
            GlobalAllocator<WeakList<T>>.DeAllocate(ins);
        }
        public static void DeAllocate(ref WeakList<T> ins)
        {
            GlobalAllocator<WeakList<T>>.DeAllocate(ins);
            ins = null;
        }

        #region Imp
        public void Add(T ins)
        {
            if(ins != null)
            {
                _refList.Add(WeakReference<T>.Allocate(ins));
            }
        }

        public bool Remove(T ins)
        {
            if(ins != null)
            {
                int hashCode = ins.GetHashCode();
                for(int i = _refList.Count - 1; i >= 0; i--)
                {
                    if(_refList[i].EqualsTo(hashCode))
                    {
                        RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// inefficient in mosy cases
        /// </summary>
        /// <param name="ins"></param>
        /// <returns></returns>
        public bool Contains(T ins)
        {
            if(ins != null)
            {
                int hashCode = ins.GetHashCode();
                for(int i = 0; i < _refList.Count; i++)
                {
                    if(_refList[i].EqualsTo(hashCode))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Dispose()
        {
            Reset();
        }

        public bool MoveNext()
        {
            return (++_index) < Count;
        }

        public void Reset()
        {
            _index = -1;
        }

        public int IndexOf(T item)
        {
            if(item != null)
            {
                int hashCode = item.GetHashCode();
                for(int i = 0; i < _refList.Count; i++)
                {
                    if(_refList[i].EqualsTo(hashCode))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            if(index >= 0 && index <= _refList.Count)
            {
                _refList.Insert(index, WeakReference<T>.Allocate(item));
            }
        }

        public void Clear()
        {
            if (_refList.Count > 0)
            {
                foreach(var weak in _refList)
                {
                    weak.Object = null;
                    WeakReference<T>.DeAllocate(weak);
                }
                _refList.Clear();
            }        
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if(array != null && array.Length > arrayIndex && Count > arrayIndex)
            {
                int count = Math.Min(array.Length, Count) - arrayIndex;
                for(int i = 0; i < count; i++)
                {
                    array[arrayIndex + i] = this[i];
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }
        #endregion

        #region List Funcs
        public void AddRange(IEnumerable<T> collection)
        {
            if(collection != null)
            {
                using(IEnumerator<T> enumerator = collection.GetEnumerator())
                {
                    while(enumerator.MoveNext())
                    {
                        this.Add(enumerator.Current);
                    }
                }
            }
        }

        public void RemoveAt(int index)
        {
            _refList.RemoveAt(index);
        }

        public void RemoveRange(int index, int count)
        {
            _refList.RemoveRange(index, count);
        }

        public bool Replace(int index, T ins)
        {
            if(index >= 0 && index < _refList.Count)
            {
                var tag = _refList[index];
                if(tag == null)
                {
                    _refList[index] = WeakReference<T>.Allocate(ins);
                }
                else
                {
                    tag.SetTarget(ins);
                }
                return true;
            }
            return false;
        }

        public int Replace(T oldItem, T newItem, ref int hash)
        {
            for (int i = 0, imax = _refList.Count; i < imax; i++)
            {
                var tag = _refList[i];
                if(tag.Object == oldItem)
                {
                    hash = tag.hash;
                    tag.SetTarget(newItem);
                    return i;
                }
            }
            return -1;
        }

        public T[] ToArray()
        {
            if(Count > 0)
            {
                T[] retVal = new T[Count];
                CopyTo(retVal, 0);
                return retVal;
            }
            return null;
        }
        #endregion

        #region Not Imp
        object IEnumerator.Current
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        #endregion

    }
}