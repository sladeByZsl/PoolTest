using UnityEngine;
using System.Collections;
using ELEX.NewPool;
using System.Collections.Generic;

namespace PoolManagerExampleFiles
{
    public class Item : IPoolItem
    {
        private static int ID = 0;
        public int id;
        public Vector3 pos;



        public Item()
        {
            id = ID ++;
        }


        /** 对象池的名称描述 */
        public string PName{ get; set;}

        /** 销毁 */
        public void PDestruct()
        {
            Debug.LogFormat("PDestruct {0}", this);
        }

        /** 对象池设置--该对象是否激活 */
        public void PSetActive(bool value)
        {
            Debug.LogFormat("PSetActive {0}", this);
        }

        /** 对象池设置--该对象重设参数 */
        public void PSetArg(params object[] args)
        {
            if (args.Length > 0)
            {
                pos = (Vector3) args[0];
            }

            Debug.LogFormat("PSetArg {0}", this);
        }


        public override string ToString()
        {
            return string.Format("[Item] id={0}, PName={1},  pos={2}", id, PName,  pos);
        }
    }
    public class ItemExample : MonoBehaviour {
        

        ObjectPool<Item> pool = new ObjectPool<Item>();
        public int count;
        public int spawned;
        public int despawned;
        public string status = "";
        public string current = "";
        public List<Item> list = new List<Item>();
    	void Start ()
        {
            StartCoroutine(TestCache());
    	}

        void Update()
        {
            count = pool.totalCount;
            spawned = pool.spawned.Count;
            despawned = pool.despawned.Count;
        }



        public IEnumerator TestCache()
        {
            // 是否打印日志信息
            pool.logMessages = true;


            PoolManager.Instance.common.CreatePool<Item>(pool);
            status = "Init";

            yield return new WaitForSeconds(5);
            for(int i = 0; i < 20; i ++)
            {
                Debug.LogFormat("-----Spawn {0}----", i);
                status = "Spawn ";
                for(int j = 0; j < 10; j ++)
                {
                    Item item = PoolManager.Instance.common.Spawn<Item>(Vector3.one * j);
                    list.Add(item);
                    Debug.LogFormat("[Spawn] {0}, {1}" , j, item);
                    Debug.Log(pool);
                    status = "Spawn " + j;
                    current =item != null ?  item.ToString() : "null";
                    yield return new WaitForSeconds(1);
                }
                yield return new WaitForSeconds(5);


                Debug.Log("-----Despawn 1----");
                for(int j = list.Count - 1; j >= 0; j --)
                {
                    Item item = list[j];
                    PoolManager.Instance.common.Despawn<Item>(item);
                    Debug.Log(pool);
                    status = "Despawn " + j;
                    current =item != null ?  item.ToString() : "null";
                    yield return new WaitForSeconds(1);
                }
                list.Clear();

                yield return new WaitForSeconds(2);
            }
        }



    }
}