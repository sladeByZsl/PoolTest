using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.ObjectPool
{
    using AssetBundleMaster.Extention;
    using AssetBundleMaster.Common;

    public class UnityObjectPool : MonoBehaviour
    {
        private string _PoolName = string.Empty;
        public string PoolName
        {
            get { return _PoolName; }
            set { if(string.IsNullOrEmpty(_PoolName)) { _PoolName = value; } }
        }
        public bool destroied { get; private set; }

        // the reference targets that use weakreference
        protected Dictionary<string, WeakReference<UnityEngine.Object>> m_unityObjs = new Dictionary<string, WeakReference<UnityEngine.Object>>();

        #region Main Funcs
        /// <summary>
        /// Add a reference to pool
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <param name="obj"></param>
        public virtual bool Add(string uniqueName, UnityEngine.Object obj)
        {
            if(obj)
            {
                var weakObj = m_unityObjs.GetValue(uniqueName, () => { return WeakReference<UnityEngine.Object>.Allocate(); });
                weakObj.SetTarget(obj);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get target asset from pool
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uniqueName"></param>
        /// <returns></returns>
        public T Get<T>(string uniqueName) where T : UnityEngine.Object
        {
            var weakObj = m_unityObjs.TryGetValue(uniqueName);
            if(weakObj != null)
            {
                return weakObj.Target as T;
            }
            return null;
        }

        /// <summary>
        /// Check resource exists
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <returns></returns>
        public bool Contains(string uniqueName)
        {
            var weakReference = m_unityObjs.TryGetValue(uniqueName);
            if(weakReference != null)
            {
                return weakReference.Object;
            }
            return false;
        }

        /// <summary>
        /// Remove reference of target
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <returns></returns>
        public bool Remove(string uniqueName)
        {
            return m_unityObjs.Remove(uniqueName);
        }

        /// <summary>
        /// destroy this pool
        /// </summary>
        public virtual void DoDestroy()
        {
            destroied = true;
            UnityComponentExtention.Destroy(this.gameObject);
        }

        /// <summary>
        /// do access all unique names
        /// </summary>
        /// <param name="keyAccess"></param>
        public Dictionary<string, WeakReference<UnityEngine.Object>>.KeyCollection GetObjectNames()
        {
            return m_unityObjs.Keys;
        }
        #endregion

        protected virtual void OnDestroy()
        {
            m_unityObjs.Clear();
        }
    }
}

