/****************************************************************************
 * Copyright (c) 2022.3 liangxiegame UNDER MIT LICENSE
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System.Collections.Generic;

namespace QFramework
{
    public class ViewControllerCodeInfo : ICodeInfo
    {
        public string GameObjectName;
        public Dictionary<string, string> DicNameToFullName = new Dictionary<string, string>();
        public readonly List<ElementCodeInfo> ElementCodeDatas = new List<ElementCodeInfo>();
        public List<BindInfo> BindInfos { get; } = new List<BindInfo>();
    }
}