/****************************************************************************
 * Copyright (c) 2022.3 liangxiegame UNDER MIT LICENSE
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;
using System.IO;
using UnityEngine;

namespace QFramework
{
    public static class CodeGenUtil
    {


        public static bool IsViewController(this Component component)
        {
            if (component.GetComponent<ViewController>())
            {
                return true;
            }

            return false;
        }

        public static string GetBindBelongs2(AbstractBind bind)
        {
            var trans = bind.Transform;
            
            while (trans.parent != null)
            {
                if (trans.parent.IsViewController())
                {
                    return trans.parent.name + "(" +  trans.parent.GetComponent<ViewController>().ScriptName  + ")";
                }
                
                if (trans.parent.GetComponent("UIPanel"))
                {
                    return "UIPanel" + "(" +trans.parent.name + ")";
                }


                trans = trans.parent;
            }

            return trans.name;
        }

        public static GameObject GetBindBelongs2GameObject(AbstractBind bind)
        {
            var trans = bind.Transform;
            
            while (trans.parent != null)
            {
                if (trans.parent.IsViewController() || trans.parent.GetComponent("UIPanel"))
                {
                    return trans.parent.gameObject;
                }

                trans = trans.parent;
            }

            return bind.gameObject;
        }


        
        /// <summary>
        /// 创建新的文件夹,如果存在则不创建
        /// <code>
        /// var testDir = "Assets/TestFolder";
        /// testDir.CreateDirIfNotExists();
        /// // 结果为，在 Assets 目录下创建 TestFolder
        /// </code>
        /// </summary>
        public static string CreateDirIfNotExistsUIKit(this string dirFullPath)
        {
            if (!Directory.Exists(dirFullPath))
            {
                Directory.CreateDirectory(dirFullPath);
            }

            return dirFullPath;
        }
    }
}