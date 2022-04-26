using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace AssetBundleMaster.Extention
{
    /// <summary>
    /// AssetBundleMaster.Common Extention for Unity Components
    /// </summary>
    public static class UnityComponentExtention
    {
        /// <summary>
        /// Get Target Component in Transform layer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="trans"></param>
        /// <param name="layerStr"></param>
        /// <returns></returns>
        public static T FindComponentInChild<T>(this Transform trans, string layerStr) where T : Component
        {
            var tagTrans = trans.Find(layerStr);
            if(tagTrans)
            {
                return tagTrans.GetComponent<T>();
            }
            return null;
        }
        /// <summary>
        /// Get Target Component in GameObject layer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <param name="layerStr"></param>
        /// <returns></returns>
        public static T FindComponentInChild<T>(this GameObject go, string layerStr) where T : Component
        {
            return go.transform.FindComponentInChild<T>(layerStr);
        }

        /// <summary>
        /// RequireComponent on gameObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <returns></returns>
        public static T RequireComponent<T>(this GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if(comp == false)
            {
                comp = go.AddComponent<T>();
            }
            return comp;
        }

        /// <summary>
        /// RequireComponent in child
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="trans"></param>
        /// <param name="layerStr"></param>
        /// <returns></returns>
        public static T RequireComponentInChild<T>(this Transform trans, string layerStr) where T : Component
        {
            var tag = RequireChild(trans, layerStr);
            return tag.gameObject.RequireComponent<T>();
        }

        /// <summary>
        /// Get Components In top children
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="trans"></param>
        /// <returns></returns>
        public static List<T> GetComponentsInTopChildren<T>(this Transform trans) where T : Component
        {
            List<T> list = new List<T>();
            for(int i = 0; i < trans.childCount; i++)
            {
                var comp = trans.GetChild(i).GetComponent<T>();
                if(comp)
                {
                    list.Add(comp);
                }
            }
            return list;
        }

        /// <summary>
        /// Require target child
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="layerStr"></param>
        /// <returns></returns>
        public static Transform RequireChild(this Transform trans, string layerStr)
        {
            var tag = trans.Find(layerStr);
            if(tag == false)
            {
                tag = trans;
                if(string.IsNullOrEmpty(layerStr) == false)
                {
                    var sp = layerStr.Split('/');
                    if(sp != null && sp.Length > 0)
                    {
                        for(int i = 0; i < sp.Length; i++)
                        {
                            var name = sp[i];
                            var childTag = tag.Find(name) ?? new GameObject(name).transform;
                            childTag.SetParent(tag);
                            tag = childTag;
                        }
                    }
                }
            }
            return tag;
        }

        /// <summary>
        /// Destroy Imp for play or not mode
        /// </summary>
        /// <param name="target"></param>
        public static void Destroy(UnityEngine.Object target)
        {
            if(target)
            {
                if(Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(target);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }
    }
}