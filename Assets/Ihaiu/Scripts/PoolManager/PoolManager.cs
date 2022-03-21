using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ELEX.NewPool
{
    public class PoolManager:MonoBehaviour
    {
        private Dictionary<string, PoolGroup> _groups = new Dictionary<string, PoolGroup>();
        
        private static PoolManager _instance;
        internal static PoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = GameObject.Find("PoolManager");
                    if (go == null)
                    {
                        go = new GameObject("PoolManager");
                    }

                    _instance = go.AddComponent<PoolManager>();
                }
                return _instance;
            }
        }
        
        void Awake()
        {
            if (_instance != null)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        void Update()
        {
            foreach (KeyValuePair<string, PoolGroup> pair in this._groups)
            {
                pair.Value.OnUpdate();
            }
        }

        /** 创建PoolGroup */
        public PoolGroup Create(string groupName)
        {
            PoolGroup poolGroup = new PoolGroup(groupName);
            poolGroup.OnStart();

            return poolGroup;
        }

        /** 销毁PoolGroup */
        public bool RemoveGroup(string groupName)
        {
            PoolGroup poolGroup;
            if (!this._groups.TryGetValue(groupName, out poolGroup))
            {
                Debug.LogError(
                    string.Format("PoolManager: Unable to destroy '{0}'. Not in PoolManager",
                        groupName));
                return false;
            }


            poolGroup.OnDestruct();
            return true;
        }

        /** 销毁所有PoolGroup */
        public void RemoveAllGroup()
        {
            foreach (KeyValuePair<string, PoolGroup> pair in this._groups)
                pair.Value.OnDestruct();

            this._groups.Clear();
        }
        
        internal void Add(PoolGroup group)
        {
            // Don't let two pools with the same name be added. See error below for details
            if (this.ContainsKey(group.groupName))
            {
                Debug.LogError(string.Format("PoolManager.Add(PoolGroup group), 已经存在groupName={0}的PoolGroup",
                    group.groupName));
                return;
            }
            this._groups.Add(group.groupName, group);
        }

        internal bool Remove(PoolGroup group)
        {
            if (!this.ContainsKey(group.groupName))
            {
                Debug.LogError(string.Format("PoolManager: Unable to remove '{0}'. " +
                    "GroupPool not in PoolManager",
                    group.groupName));
                return false;
            }

            this._groups.Remove(group.groupName);
            return true;
        }

        #region Dict Functionality
        public int Count { get { return this._groups.Count; } }

        /** 是否已经存在groupName的PoolGroup */
        public bool ContainsKey(string groupName)
        {
            return this._groups.ContainsKey(groupName);
        }

        /** 尝试获取groupName的PoolGroup */
        public bool TryGetValue(string groupName, out PoolGroup poolGroup)
        {
            return this._groups.TryGetValue(groupName, out poolGroup);
        }

        /** 获取key的PoolGroup */
        public PoolGroup this[string key]
        {
            get
            {
                PoolGroup group;
                try
                {
                    group = this._groups[key];
                }
                catch (KeyNotFoundException)
                {
                    string msg = string.Format("A PoolGroup with the name '{0}' not found. " +
                        "\nPools={1}",
                        key, this.ToString());
                    throw new KeyNotFoundException(msg);
                }

                return group;
            }
            set
            {
                string msg = "Cannot set PoolManager.Group[key] directly. " +
                    "SpawnPools add themselves to PoolManager.Pools when created, so " +
                    "there is no need to set them explicitly. Create pools using " +
                    "PoolManager.Pools.Create() or add a SpawnPool component to a " +
                    "GameObject.";
                throw new System.NotImplementedException(msg);
            }
        }
        #endregion Dict Functionality
        
        /** 默认PoolGroup，可以自己扩展 */
        public PoolGroup common
        {
            get
            {
                if (!ContainsKey("CommonPoolGroup"))
                {
                    Create("CommonPoolGroup");
                }

                return this["CommonPoolGroup"];
            }
        }
    }
}