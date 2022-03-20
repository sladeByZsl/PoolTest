using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMono : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable()
    {
        Debug.LogError("onEnable");
    }
    
    private void OnDisable()
    {
        Debug.LogError("OnDisable");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
