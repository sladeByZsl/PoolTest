using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AssetBundleMaster.ContainerUtilities
{
    using AssetBundleMaster.Common;

    #region Json Serialization
    [Serializable]
    public class BaseJsonSerialization : ISerializationCallbackReceiver
    {
        public virtual void OnBeforeSerialize()
        {
        }

        public virtual void OnAfterDeserialize()
        {
        }

        /// <summary>
        /// Serialize it to Json String
        /// </summary>
        /// <returns></returns>
        public virtual string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }
    /// <summary>
    /// Serializable BestList
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class JsonUtilitySerializationList<T> : BaseJsonSerialization
    {
        [SerializeField]
        public List<T> data = null;

        public JsonUtilitySerializationList()
        {
            data = new List<T>();
        }
        public JsonUtilitySerializationList(List<T> target)
        {
            this.data = target;
        }

        public static JsonUtilitySerializationList<T> FromJson(string jsonStr)
        {
            if(string.IsNullOrEmpty(jsonStr) == false)
            {
                return JsonUtility.FromJson<JsonUtilitySerializationList<T>>(jsonStr);
            }
            return null;
        }
    }
    /// <summary>
    /// Serializable Dictionary
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Serializable]
    public class JsonUtilitySerializationDictionary<TKey, TValue> : BaseJsonSerialization
    {
        [SerializeField]
        List<TKey> _keys = new List<TKey>();
        [SerializeField]
        List<TValue> _values = new List<TValue>();

        Dictionary<TKey, TValue> _target = new Dictionary<TKey, TValue>();
        public Dictionary<TKey, TValue> data
        {
            get
            {
                return _target;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue val;
                if(false == _target.TryGetValue(key, out val))
                {
                    throw new System.ArgumentOutOfRangeException("key no exists");
                }
                return val;
            }
            set
            {
                _target[key] = value;
            }
        }

        public JsonUtilitySerializationDictionary()
        {
        }
        public JsonUtilitySerializationDictionary(Dictionary<TKey, TValue> target)
        {
            this._target = target;
        }

        #region Implement Funcs
        public override void OnBeforeSerialize()
        {
            _keys = new List<TKey>(_target.Keys);
            _values = new List<TValue>(_target.Values);
        }
        public override void OnAfterDeserialize()
        {
            var count = Math.Min(_keys.Count, _values.Count);
            _target = new Dictionary<TKey, TValue>();
            for(var i = 0; i < count; ++i)
            {
                _target[_keys[i]] = _values[i];
            }
        }
        #endregion

        public static JsonUtilitySerializationDictionary<TKey, TValue> FromJson(string jsonStr)
        {
            if(string.IsNullOrEmpty(jsonStr) == false)
            {
                return JsonUtility.FromJson<JsonUtilitySerializationDictionary<TKey, TValue>>(jsonStr);
            }
            return null;
        }
    }

    public static class JsonSerializationHelper
    {
        /// <summary>
        /// Create From Json
        /// </summary>
        /// <typeparam name="G"></typeparam>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public static T CreateFromJson<T>(string jsonStr) where T : BaseJsonSerialization
        {
            if(string.IsNullOrEmpty(jsonStr) == false)
            {
                return JsonUtility.FromJson<T>(jsonStr);
            }
            return null;
        }
        /// <summary>
        /// Create From Json File
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static T CreateFromJsonFile<T>(string filePath) where T : BaseJsonSerialization
        {
            if(System.IO.File.Exists(filePath))
            {
                return JsonUtility.FromJson<T>(System.IO.File.ReadAllText(filePath));
            }
            return null;
        }
        /// <summary>
        /// Save as json
        /// </summary>
        /// <param name="data"></param>
        /// <param name="savePath"></param>
        public static void SaveJsonSerialization(BaseJsonSerialization data, string savePath)
        {
            FileAccessWrappedFunction.WriteStringToFile(savePath, data.ToJson(), false);
        }
    }
    #endregion
}