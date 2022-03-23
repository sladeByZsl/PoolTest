using UnityEngine;
using System.Collections;

namespace ELEX.NewPool
{
    public abstract class AbstractObjectPool 
    {
        public PoolGroup poolGroup;

        public string name { get; set;}

        #region 配置属性
        
        /** 是否开启对象实例化的限制功能。 */
        public bool limitInstances = false;

        /** 限制实例化Prefab的数量，也就是限制缓冲池的数量，它和上面的preloadAmount是有冲突的，如果同时开启则以limitAmout为准。 */
        public int limitAmount = 100;

        /** 如果我们限制了缓存池里面只能有10个Prefab，如果不勾选它，那么你拿第11个的时候就会返回null。如果勾选它在取第11个的时候他会返回给你前10个里最不常用的那个。*/
        public bool limitFIFO = false;
        
        /** 是否开启缓存池智能自动清理模式。*/
        public bool autoDestroy = true;

        /** 缓存池自动清理，但是始终保留几个对象不清理。 */
        public int holdNum = 5;

        /** 每过多久执行一遍自动清理，单位是秒。从上一次清理过后开始计时 */
        public int autoDestorySpan = 2;

        /** 每次自动清理几个游戏对象。 */
        public int destoryNumPerFrame = 2;

        /** 是否打印日志信息 */
        public bool logMessages = true;
        #endregion

        virtual internal void inspectorInstanceConstructor()
        {
            
        }
        /// <summary>
        /// 析构方法
        /// 用来清理数据
        /// </summary>
        virtual internal void SelfDestruct()
        {
            
        }
        virtual internal void OnUpdate()
        {
            
        }
    }
}