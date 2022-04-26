using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.Common
{
    [System.Serializable]
    public class SerialiserableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();

        [SerializeField]
        private List<TValue> values = new List<TValue>();

        // save the dictionary to lists
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach(KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        // load dictionary from lists
        public void OnAfterDeserialize()
        {
            this.Clear();
            int minCount = Mathf.Min(keys != null ? keys.Count : 0, values != null ? values.Count : 0);
            for(int i = 0; i < minCount; i++)
            {
                this.Add(keys[i], values[i]);
            }
        }
    }
}
