using UnityEngine;
using System.Collections;

namespace AssetBundleMaster.GameUtilities
{
    using AssetBundleMaster.Extention;

    /// <summary>
    /// This Create Single Component that will auto init
    /// reducing init call for all single componets
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <summary>
    /// singleton component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SingletonComponent<T> : MonoBehaviour where T : SingletonComponent<T>
    {
        protected static T _instance = null;                // instance reference
        public static bool Inited = false;
        private static bool _independent = true;            // mark for destroy

        /// <summary>
        /// Implict Create and get instance
        /// </summary>
        public static T Instance
        {
            get
            {
                return Create();
            }
        }

        #region Interface
        // init / uninit function
        protected virtual void Initialize() { }
        protected virtual void UnInitialize() { }
        #endregion

        #region Main Funcs
        /// <summary>
        /// Explict Create and get instance
        /// </summary>
        /// <returns></returns>
        public static T Create(Transform parent = null, GameObject attatchdTag = null)
        {
            if(_instance == false)
            {
                GameObject go = null;
                if(AssetBundleMaster.Common.GlobalVariable.isRuntimeMode == false)
                {
                    var tag = GameObject.FindObjectOfType<T>();
                    if(tag)
                    {
                        go = tag.gameObject;
                    }
                }
                _independent = (attatchdTag == false);
                if(go == false)
                {
                    go = attatchdTag ? attatchdTag : new GameObject(string.Concat("[", typeof(T).Name, "]"));
                }
                _instance = go.RequireComponent<T>();                   // awake before set instance
                if(parent)
                {
                    _instance.transform.SetParent(parent, true);
                }
                _instance.InternalInitialize();
            }
            return _instance;
        }

        /// <summary>
        /// please implement UnInitialize for release subclass datas
        /// </summary>
        public static void Destroy()
        {
            if(_instance)
            {
                _instance.UnInitialize();
                if(_independent)
                {
                    UnityComponentExtention.Destroy(_instance.gameObject);
                }
                else
                {
                    UnityComponentExtention.Destroy(_instance);
                }
                Inited = false;
                _instance = null;
            }
        }
        #endregion

        #region Mono Funcs
        /// <summary>
        /// awake for keeping singleton mode effective, in some causes like Clone / Copy
        /// </summary>
        protected virtual void Awake()
        {
            if(_instance == false)
            {
                _instance = this.gameObject.GetComponent<T>();
                InternalInitialize();
            }
        }
        #endregion

        #region Help Funcs
        // wrapped do init
        private void InternalInitialize()
        {
            if(false == Inited && _instance)
            {
                _instance.Initialize();
                Inited = true;
                if(Application.isPlaying)
                {
                    Object.DontDestroyOnLoad(_instance.gameObject);               // dont destroy singleton component
                }
            }
        }
        #endregion

    }
}

