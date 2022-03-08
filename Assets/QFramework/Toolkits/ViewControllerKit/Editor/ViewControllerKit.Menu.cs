/****************************************************************************
 * Copyright (c) 2022.3 liangxiegame UNDER MIT LICENSE
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QFramework
{
    public class ViewControllerKitMenu
    {
        [MenuItem("GameObject/@ViewControllerKit - Add Bind (alt + b) &b",false,-1)]
        public static void AddBind()
        {
            foreach (var gameObject in Selection.objects.OfType<GameObject>())
            {
                if (gameObject)
                {
                    var bind = gameObject.GetComponent<Bind>();

                    if (!bind)
                    {
                        gameObject.AddComponent<Bind>();
                    }

                    EditorUtility.SetDirty(gameObject);
                    EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
            }
        }
    }
}