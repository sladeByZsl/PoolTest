using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.Common
{
    public static class Containers
    {
        public class OrderedDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerator<KeyValuePair<TKey, TValue>>, System.IDisposable
        {
            private List<TKey> keys = new List<TKey>();
            private Dictionary<TKey, TValue> table = new Dictionary<TKey, TValue>();

            public TValue this[int index]
            {
                get
                {
                    if(index < keys.Count)
                    {
                        return table[keys[index]];
                    }
                    return default(TValue);
                }
            }

            public int Count { get { return keys.Count; } }

            public void Add(TKey key, TValue value, bool keepSequence = true)
            {
                if(table.ContainsKey(key) == false)
                {
                    keys.Add(key);
                }
                else
                {
                    if(false == keepSequence)
                    {
                        keys.Remove(key);
                        keys.Add(key);
                    }
                }
                table[key] = value;
            }

            // kind of slow
            public bool Remove(TKey key)
            {
                if(table.Remove(key))
                {
                    keys.Remove(key);
                    return true;
                }
                return false;
            }

            public bool RemoveAt(int index)
            {
                if(keys.Count > index)
                {
                    var key = keys[index];
                    keys.RemoveAt(index);
                    table.Remove(key);
                    return true;
                }
                return false;
            }

            // slow
            public void RemoveIndexes(List<int> indexes)
            {
                if(this.Count > 0 && indexes != null)
                {
                    indexes.Sort((_l, _r) =>
                    {
                        if(_l > _r)
                        {
                            return 1;
                        }
                        if(_l < _r)
                        {
                            return -1;
                        }
                        return 0;
                    });
                    for(int i = indexes.Count - 1; i >= 0; i--)
                    {
                        int index = indexes[i];
                        RemoveAt(index);
                    }
                }
            }

            public bool TryGetValue(TKey key, out TValue value)
            {
                return table.TryGetValue(key, out value);
            }

            public TValue TryGetValue(TKey key)
            {
                TValue value = default(TValue);
                if(table.TryGetValue(key, out value))
                {
                    return value;
                }
                return default(TValue);
            }

            public void Clear()
            {
                keys.Clear();
                table.Clear();
            }

            public void Foreach(System.Action<TKey, TValue> access)
            {
                foreach(var kv in table)
                {
                    access.Invoke(kv.Key, kv.Value);
                }
            }

            public void Iterate(System.Func<TKey, TValue, bool> access)
            {
                for(int i = 0; i < keys.Count; i++)
                {
                    var key = keys[i];
                    var value = table[key];
                    if(access.Invoke(key, value) == false)
                    {
                        return;
                    }
                }
            }

            #region Imp Interface
            private int _index = -1;
            public int currentIndex { get { return _index; } }

            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    var tag = keys[_index];
                    return new KeyValuePair<TKey, TValue>(tag, table[tag]);
                }
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return this;
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
            #endregion

            #region Not Imp Interface
            object IEnumerator.Current
            {
                get { throw new System.NotImplementedException(); }
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new System.NotImplementedException();
            }
            #endregion
        }

        public static TValue GetValue<TKey, TValue>(this OrderedDictionary<TKey, TValue> table, TKey key) where TValue : new()
        {
            TValue value;
            if(table.TryGetValue(key, out value) == false || value == null)
            {
                value = new TValue();
                table.Add(key, value);
            }
            return value;
        }

        public static void RemoveAll<TKey, TValue>(this OrderedDictionary<TKey, TValue> dict, System.Func<TKey, TValue, bool> predicate)
        {
            var removeList = AssetBundleMaster.ObjectPool.GlobalAllocator<List<int>>.Allocate();
            foreach(var data in dict)
            {
                if(predicate.Invoke(data.Key, data.Value))
                {
                    removeList.Add(dict.currentIndex);
                }
            }
            if(removeList.Count > 0)
            {
                for(int i = removeList.Count - 1; i >= 0; i--)
                {
                    int index = removeList[i];
                    dict.RemoveAt(index);
                }
                removeList.Clear();
            }
            AssetBundleMaster.ObjectPool.GlobalAllocator<List<int>>.DeAllocate(removeList);
        }
    }
}