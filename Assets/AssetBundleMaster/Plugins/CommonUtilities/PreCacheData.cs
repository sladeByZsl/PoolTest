using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.Common
{
    public class PreCacheData<T>
    {
        private T _data = default(T);
        private System.Func<T> _defaultVal = null;
        private bool _get = false;

        public T data
        {
            get
            {
                if(_get == false)
                {
                    _get = true;
                    if(_defaultVal != null)
                    {
                        _data = _defaultVal.Invoke();
                    }
                }
                return _data;
            }
            set
            {
                if(_get == false || value.Equals(_data) == false)
                {
                    _get = true;
                    _data = value;
                }
            }
        }

        public PreCacheData(System.Func<T> defaultVal)
        {
            _defaultVal = defaultVal;
        }

        public T GetData()
        {
            return data;
        }

        public static implicit operator string(PreCacheData<T> data)
        {
            return data.data.ToString();
        }
    }

}