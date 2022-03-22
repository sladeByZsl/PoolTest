using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ELEX.NewPool
{
    public class NewComponentPool<T> : ObjectPool<T> where T : Component
    {
        public Transform parent;
        private GameObject _prefabGO;
        private T prefabComp;
        internal GameObject prefabGO
        {
            get
            {
                if (_prefabGO == null)
                {
                    _prefabGO = new GameObject("Component_templete_"+typeof(T));
                    _prefabGO.SetActive(false);
                    prefabComp=_prefabGO.AddComponent<T>();
                }
                return _prefabGO;
            }
        }

        private bool _dontDestroyOnLoad;
        public bool dontDestroyOnLoad
        {
            get
            {
                return this._dontDestroyOnLoad;
            }

            set
            {
                this._dontDestroyOnLoad = value;

                if (this.parent != null)
                    Object.DontDestroyOnLoad(this.parent.gameObject);
            }
        }

        internal override void inspectorInstanceConstructor()
        {
            base.inspectorInstanceConstructor();
            
            if (parent == null)
            {
                parent = new GameObject(prefabGO.name.Replace("(Clone)", "") + "Pool").transform;
            }

            prefabGO.transform.parent = parent;

            if (dontDestroyOnLoad)
            {
                Object.DontDestroyOnLoad(this.parent.gameObject);
            }
        }

        internal override void SelfDestruct()
        {
            base.SelfDestruct();
            if (_prefabGO != null)
            {
                Object.Destroy(_prefabGO);
            }
            if (parent != null)
            {
                Object.Destroy(parent.gameObject);
            }
        }

        
        /** 调用对象方法--销毁 */
        protected override void ItemDestruct(T instance)
        {
            Object.Destroy(instance.gameObject);
        }

        /** 调用对象方法--设置是否激活 */
        protected override void ItemSetActive(T instance, bool value)
        {
            instance.gameObject.SetActive(value);
        }

        /** 调用对象方法--实例对象重设参数 */
        protected override void ItemSetArg(T instance, params object[] args)
        {
            instance.gameObject.BroadcastMessage(
                "SetArg",
                args,
                SendMessageOptions.DontRequireReceiver
            );
        }

        /** 给实例对象重命名 */
        protected override void nameInstance(T instance)
        {
            instance.gameObject.name = instance.name.Replace("(Clone)", string.Empty) + (this.index).ToString("#000");
        }

        /** 实例化一个对象 */
        protected override T Instantiate(params object[] args)
        {
            Transform instance = GameObject.Instantiate(prefabGO).transform;
            if(parent != null) instance.SetParent(parent, false);
            T t = instance.GetComponent<T>();
            ItemSetArg(t, args);
            return t;
        }
    }
}
