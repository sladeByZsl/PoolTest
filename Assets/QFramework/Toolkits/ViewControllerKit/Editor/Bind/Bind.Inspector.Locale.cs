/****************************************************************************
 * Copyright (c) 2022.3 liangxiegame UNDER MIT LICENSE
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

namespace QFramework
{
    public class BindInspectorLocale
    {
        public bool CN
        {
            get => ViewControllerKit.Setting.CN;
            set => ViewControllerKit.Setting.CN = value;
        }
        
        public string Type => CN ? " 类型:" : " Type:";
        public string Comment => CN ? " 注释" : " Comment";
        public string BelongsTo => CN ? " 属于:" : " Belongs 2:";
        public string Select => CN ? "选择" : "Select";
        public string Generate => CN ? " 生成代码" : " Generate Code";

        public string Bind => CN ? " 绑定设置" : " Bind Setting";
    }
}