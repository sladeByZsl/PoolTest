using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
        //一.数据模块
        //1.用二维数组初始化华容道二维数组对应的内容即为显示的数字，0表示空位
        int index = 0;
        int[,] data=new int[3,3];
        for (int i=0;i<3;i++)
        {
            for (int j=0;j<3;j++)
            {
                data[i, j] = index++;
            }
        }
        
        //2.打乱二维数组
        
        //二.表现模块
        //1.根据二维数组，初始化华容道的item，空白位置置空
        
        
        //2.输入处理
        //点击时触发数据交换,item交换
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
