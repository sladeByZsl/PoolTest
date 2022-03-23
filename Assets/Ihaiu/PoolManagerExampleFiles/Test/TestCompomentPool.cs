using System.Collections;
using System.Collections.Generic;
using ELEX.NewPool;
using UnityEngine;

public class TestCompomentPool : MonoBehaviour
{
    NewComponentPool<TestMono> _pool=new NewComponentPool<TestMono>();
    
    List<TestMono> list = new List<TestMono>();
    public int count;
    public int spawned;
    public int despawned;
    void Start()
    {
        PoolManager.Instance.common.CreatePool(_pool);
    }
    
    void Update()
    {
        count = _pool.totalCount;
        spawned = _pool.spawned.Count;
        despawned = _pool.despawned.Count;
        
        if (Input.GetKeyDown(KeyCode.J))
        {
            //1.这种方式，如果你没有对象池，可以帮你自动创建一个ObjectPool的对象池
            //TestMono t = PoolManager.Instance.common.Spawn<TestMono>();
            
            //2.这种方式，也可以
            TestMono t = _pool.SpawnInstance();
            list.Add(t);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            if (list.Count > 0)
            {
                TestMono item = list[0];
                list.RemoveAt(0);
                _pool.DespawnInstance(item);
            }
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            //1.这种方式，如果你没有对象池，可以帮你自动创建一个ObjectPool的对象池
            PoolManager.Instance.common.ClearAllDespawn<TestMono>();
            
            //2.直接清理
            _pool.ClearAllDespawn();
        }
    }
}
