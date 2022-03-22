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
            TestMono t = PoolManager.Instance.common.Spawn<TestMono>();
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
            PoolManager.Instance.common.ClearPool<TestMono>();
        }
    }
}
