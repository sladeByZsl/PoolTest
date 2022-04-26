using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AssetBundleMaster.Common
{
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        protected static T _instance;       // instance reference
        public static bool Inited = false;

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

        #region Interfaces
        // abstract method
        protected virtual void Initialize() { }
        protected virtual void UnInitialize() { }
        #endregion

        #region Main Funcs
        /// <summary>
        /// Explict Create and get instance
        /// </summary>
        /// <returns></returns>
        public static T Create()
        {
            if (_instance == null)
            {
                _instance = new T();
                _instance.InternalCreat();
            }
            return _instance;
        }

        /// <summary>
        /// call uninit and release instance
        /// </summary>
        public static void Destroy()
        {
            if (_instance != null)
            {
                _instance.UnInitialize();
                _instance = null;
                Inited = false;
            }
        }
        #endregion

        #region Help Funcs
        /// <summary>
        /// Internal Create
        /// </summary>
        private void InternalCreat()
        {
            if (_instance != null)
            {
                _instance.Initialize();     // must implement
                Inited = true;
            }
        }
        #endregion

    }
}
