/****************************************************************************
 * Copyright (c) 2017 ~ 2022.3 liangxiegame UNDER MIT LICENSE
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

namespace QFramework
{
    using UnityEngine;
    
    
    public interface IBind
    {
        string Name { get; }
        
        string Comment { get; }

        Transform Transform { get; }
    }
}