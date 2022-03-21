using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using zsl.Pool;


public class Apple
{
    public int appleID;
    public string appleKey;

    public void Init()
    {
        this.appleKey = "apple"+Time.realtimeSinceStartup;
        this.appleID = Time.frameCount;
    }
     
    public void Clear()
    {
        appleID = -1;
        appleKey = "empty";
    }
}

public class PoolTest : MonoBehaviour
{
    // OnStart is called before the first frame update
    private Pool<Apple> applePool = null;
    private Apple apple;
    void Start()
    {
        applePool = PoolManager.Instance.GetOrCreatePool<Apple>(
            () => { return new Apple(); }, 
            (Apple) =>
            {
                
            },
            new PoolConfig() {poolName = "applePool"});
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(20, 0, 100, 50), "第一个Button"))
        {
            apple = applePool.Get();
        }

        if (GUI.Button(new Rect(20, 80, 100, 50), "第二个Button"))
        {
            applePool.Recycle(apple);
        }
    }
}
