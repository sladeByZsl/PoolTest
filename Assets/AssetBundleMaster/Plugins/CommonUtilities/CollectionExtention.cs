using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AssetBundleMaster.Extention
{
    /// <summary>
    /// Ext for All System Collections
    /// </summary>
    public static class CollectionExtention
    {
        #region List<T> Extention
        /// <summary>
        /// Extention for List, switch all remove tag to tail array, and remove all, sequence will be changed maybe
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="predicate"></param>
        public static void FastRemoveAll<T>(this List<T> list, System.Func<T, bool> predicateRemove)
        {
            if(predicateRemove == null || list.Count == 0)
            {
                return;
            }
            int lastPos = list.Count - 1;
            int startPos = 0;
            int removeCount = 0;
            for(int i = 0, imax = list.Count; i < imax; i++)
            {
                // successed remove
                if(predicateRemove(list[i]))
                {
                    list[i] = list[lastPos];
                    removeCount++;
                    lastPos--;
                    imax--;
                    i--;
                }
                else
                {
                    startPos++;
                }
            }
            if(removeCount > 0)
            {
                list.RemoveRange(startPos, removeCount);
            }
        }
        /// <summary>
        /// Wrapped Force Get Value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T GetValue<T>(this List<T> list, System.Func<T, bool> tagFunc, System.Func<T> create) where T : new()
        {
            if(tagFunc != null)
            {
                for(int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if(tagFunc(item))
                    {
                        return item;
                    }
                }
            }
            T retVal = create != null ? create.Invoke() : new T();
            list.Add(retVal);
            return retVal;
        }
        /// <summary>
        /// Remove first item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="predicate"></param>
        public static void RemoveFirst<T>(this List<T> list, System.Func<T, bool> predicate)
        {
            for(int i = 0; i < list.Count; i++)
            {
                if(predicate.Invoke(list[i]))
                {
                    list.RemoveAt(i);
                    return;
                }
            }
        }
        #endregion

        #region Dictionary<K, V> Extention
        /// <summary>
        /// try get value, value is class
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TValue TryGetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : class
        {
            TValue retVal = null;
            dict.TryGetValue(key, out retVal);
            return retVal;
        }
        /// <summary>
        /// try get value, boxing value type
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TValue? TryGetValue_Nullable<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : struct
        {
            TValue? retVal = null;
            TValue val = default(TValue);
            if(dict.TryGetValue(key, out val))
            {
                retVal = val;
            }
            return retVal;
        }
        /// <summary>
        /// remove all elements, wrapped function, use temp pool for it
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="predicate"></param>
        public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dict, System.Func<TKey, TValue, bool> predicate)
        {
            var removeList = AssetBundleMaster.ObjectPool.GlobalAllocator<List<TKey>>.Allocate();
            foreach(var data in dict)
            {
                if(predicate.Invoke(data.Key, data.Value))
                {
                    removeList.Add(data.Key);
                }
            }
            if(removeList.Count > 0)
            {
                for(int i = 0; i < removeList.Count; i++)
                {
                    var key = (TKey)removeList[i];
                    dict.Remove(key);
                }
                removeList.Clear();  // clear if the references should be release
            }          
            AssetBundleMaster.ObjectPool.GlobalAllocator<List<TKey>>.DeAllocate(removeList);
        }
        /// <summary>
        /// Force get value from a dictionary, less GC
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="create"></param>
        /// <returns></returns>
        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, System.Func<TValue> create) where TValue : class
        {
            TValue retVal = null;
            if(dict.TryGetValue(key, out retVal) == false || retVal == null)
            {
                retVal = create.Invoke();
                dict[key] = retVal;
            }
            return retVal;
        }
        /// <summary>
        /// Force get value from a dictionary, create if no exists
        /// Notice: Generic create instance has GC
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : class, new()
        {
            return GetValue(dict, key, () => { return new TValue(); }); // this call has more GC
        }
        #endregion

        #region Queue<T> Extention
        /// <summary>
        /// remove from index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="index"></param>
        public static void RemoveFrom<T>(this Queue<T> queue, int index)
        {
            if(queue.Count > index)
            {
                if(index == 0)
                {
                    queue.Clear();
                }
                else
                {
                    for(int i = 0, imax = queue.Count; i < imax; i++)
                    {
                        var curElement = queue.Dequeue();
                        if(i < index)
                        {
                            queue.Enqueue(curElement);
                        }
                    }
                }
            }
        }
        #endregion

        #region Array Extention
        /// <summary>
        /// Array Contains item check ext
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool Contains<T>(this T[] array, T item)
        {
            if(item != null && array != null && array.Length > 0)
            {
                return Array.IndexOf(array, item) >= 0;
            }
            return false;
        }
        /// <summary>
        /// index of
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static int ArrayIndexOf<T>(this T[] array, T item)
        {
            if(array.Length > 0)
            {
                return Array.IndexOf(array, item);
            }
            return -1;
        }
        /// <summary>
        /// set array to target size, copy orgin to tag -- return the orgin size
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inoutArray"></param>
        /// <param name="setSize"></param>
        /// <returns></returns>
        public static int ResizeArray<T>(ref T[] inoutArray, int setSize)
        {
            int orginSize = (inoutArray == null ? -1 : inoutArray.Length);
            if(orginSize != setSize && setSize >= 0)
            {
                var finalOutArray = new T[setSize];
                if(inoutArray != null)
                {
                    Array.Copy(inoutArray, 0, finalOutArray, 0, Math.Min(inoutArray.Length, finalOutArray.Length));
                }
                inoutArray = finalOutArray;
            }
            return orginSize;
        }
        #endregion
        
    }
}
