using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace AssetBundleMaster.Common
{
    /// <summary>
    /// PlayerPrefs get/save interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PlayerPrefsData<T>
    {
        private string _saveKey;
        private T _data = default(T);
        private System.Func<T> _defaultValFunc = null;
        private System.Func<T, string> _serializeFunc = null;
        private System.Func<string, T> _deserializeFunc = null;
        private System.Func<string, string> _loadFunc = null;
        private System.Action<string> _saveFunc = null;
        private bool _get = false;

        public T data
        {
            get
            {
                if(false == _get)
                {
                    _get = true;
                    _data = Load();
                }
                return _data;
            }
            set
            {
                if(false == _get || value.Equals(_data) == false)
                {
                    _get = true;
                    _data = value;
                    Save(true);
                }
            }
        }

        public PlayerPrefsData(string saveKey, bool newTarget = true)
        {
            _saveKey = saveKey;
            if(newTarget)
            {
                _defaultValFunc = () => { return System.Activator.CreateInstance<T>(); };
            }
            else
            {
                _defaultValFunc = () => { return default(T); };
            }
        }

        public PlayerPrefsData(string saveKey, T defaultValue)
        {
            _saveKey = saveKey;
            _defaultValFunc = () => { return defaultValue; };
        }

        public PlayerPrefsData(string saveKey, System.Func<T> defaultValFunc)
        {
            _saveKey = saveKey;
            _defaultValFunc = defaultValFunc;
        }

        public PlayerPrefsData<T> Set(System.Func<T, string> serializeFunc = null, System.Func<string, T> deserializeFunc = null,
            System.Func<string, string> loadFunc = null, System.Action<string> saveFunc = null)
        {
            _serializeFunc = serializeFunc;
            _deserializeFunc = deserializeFunc;
            _loadFunc = loadFunc;
            _saveFunc = saveFunc;
            return this;
        }

        private T Load()
        {
            try
            {
                var val = _loadFunc != null ? _loadFunc.Invoke(_saveKey) : PlayerPrefs.GetString(_saveKey);
                if(string.IsNullOrEmpty(val) == false)
                {
                    if(_deserializeFunc != null)
                    {
                        return _deserializeFunc.Invoke(val);
                    }
                    var tag = (T)System.Convert.ChangeType(val, typeof(T));
                    return tag;
                }
            }
            catch
            {
                // do nothing
            }
            return _defaultValFunc.Invoke();
        }

        public void Save(bool doSave = true)
        {
            try
            {
                var saveString = _serializeFunc != null ? _serializeFunc.Invoke(data) : (_data != null ? _data.ToString() : string.Empty);
                if(_saveFunc != null)
                {
                    _saveFunc.Invoke(saveString);
                    return;
                }
                else
                {
                    PlayerPrefs.SetString(_saveKey, saveString);
                }
#if UNITY_EDITOR
                PlayerPrefsDataKeys.SaveKey(_saveKey);
#endif
            }
            finally
            {
                if(doSave)
                {
                    PlayerPrefs.Save();
                }
            }
        }
    }


#if UNITY_EDITOR
    public static class PlayerPrefsDataKeys
    {
        /* editor tools for PlayerPrefsData*/
        public const string AssetBundleMasterPlayerPrefsDatas = "AssetBundleMasterPlayerPrefsDatas";
        public const char SplitString = '#';

        private static readonly HashSet<string> ms_abmKeys = new HashSet<string>();
        private static readonly System.Text.StringBuilder ms_sb = new System.Text.StringBuilder();

        static PlayerPrefsDataKeys()
        {
            var keys = PlayerPrefs.GetString(AssetBundleMasterPlayerPrefsDatas);
            if(string.IsNullOrEmpty(keys) == false)
            {
                var sps = keys.Split(SplitString);
                if(sps != null && sps.Length > 0)
                {
                    ms_abmKeys.UnionWith(sps);
                }
            }
        }

        public static void SaveKey(string key)
        {
            if(string.IsNullOrEmpty(key) == false)
            {
                if(ms_abmKeys.Add(key))
                {
                    var saveStr = CombineKeys(ms_abmKeys);
                    PlayerPrefs.SetString(AssetBundleMasterPlayerPrefsDatas, saveStr);
                    PlayerPrefs.Save();
                }
            }
        }

        public static void DeleteAllKeys()
        {
            foreach(var key in ms_abmKeys)
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        static string CombineKeys(HashSet<string> keys)
        {
            ms_sb.Length = 0;
            foreach(var key in keys)
            {
                if(ms_sb.Length > 0)
                {
                    ms_sb.Append(SplitString);
                }
                ms_sb.Append(key);
            }
            return ms_sb.ToString();
        }
    }
#endif
}
