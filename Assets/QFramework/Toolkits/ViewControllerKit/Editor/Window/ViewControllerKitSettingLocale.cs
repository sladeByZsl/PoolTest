/****************************************************************************
 * Copyright (c) 2022.3 liangxiegame UNDER MIT LICENSE
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

namespace QFramework
{
    public class ViewControllerKitSettingLocale
    {
        public bool CN
        {
            get => ViewControllerKit.Setting.CN;
            set => ViewControllerKit.Setting.CN = value;
        }
        public string Setting => CN ? " ViewControllerKit 设置:" : " ViewControllerKit Setting:";
        public string Namespace => CN ? " 默认命名空间:" : " Namespace:";
        public string ViewControllerScriptGenerateDir => CN ? " ViewController 脚本生成路径:" : " Default ViewController Generate Dir:";
        public string ViewControllerPrefabGenerateDir =>
            CN
                ? " ViewController Prefab 生成路径:"
                : " Default ViewController Prefab Dir:";

        public string Apply => CN ? "保存" : "Apply";
    }
}