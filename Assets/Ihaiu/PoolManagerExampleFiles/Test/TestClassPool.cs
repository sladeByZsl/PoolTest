using System.Collections;
using System.Collections.Generic;
using Ihaius;
using UnityEngine;


public class TestClassItem:IPoolItem
{
    public string PName { get; set; }
    public void PDestruct()
    {
        
    }

    public void POnSpawned<T>(ObjectPool<T> pool)
    {
        Debug.LogError("POnSpawned");
    }

    public void POnDespawned<T>(ObjectPool<T> pool)
    {
        Debug.LogError("POnDespawned");
    }

    public void PSetActive(bool value)
    {
        Debug.LogError("PSetActive:"+value);
    }

    public void PSetArg(params object[] args)
    {
        string t = "aaa";
        if (args.Length > 0)
        {
            t=(string)args[0];
        }
        Debug.LogError("PSetArg:"+t);
    }
}

public class TestClassPool : MonoBehaviour
{
   ObjectPool<TestClassItem> _pool=new ObjectPool<TestClassItem>();
   
   public List<TestClassItem> list = new List<TestClassItem>();
   
   public int count;
   public int spawned;
   public int despawned;
    void Start()
    {
       
        
    }
    
    

    
    void Update()
    {
        count = _pool.totalCount;
        spawned = _pool.spawned.Count;
        despawned = _pool.despawned.Count;
        if (Input.GetKeyDown(KeyCode.J))
        {
            TestClassItem t =  _pool.SpawnInstance("canshu");
            list.Add(t);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            if (list.Count > 0)
            {
                TestClassItem item = list[0];
                list.RemoveAt(0);
                _pool.DespawnInstance(item);
            }
        }
    }
}
