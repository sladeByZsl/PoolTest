/****************************************************************************
 * Copyright (c) 2022.3 liangxiegame UNDER MIT LICENSE
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace QFramework
{
    public class ViewControllerKitSetting : ScriptableObject
    {
        public bool IsDefaultNamespace => Namespace == "QFramework.Example";

        public string Namespace = "QFramework.Example";
        
        public string ScriptDir = "Assets/Scripts/Game";
		
        public string PrefabDir = "Assets/Art/Prefab";

        public bool CN = true;

        private static ViewControllerKitSetting mInstance;
        public static ViewControllerKitSetting Load()
        {
            if (mInstance) return mInstance;
            
            var filePath = DIR.Value + FILE_NAME;
            
            if (File.Exists(filePath))
            {
                return mInstance = AssetDatabase.LoadAssetAtPath<ViewControllerKitSetting>(filePath);
            }

            return mInstance = CreateInstance<ViewControllerKitSetting>();
        }

        public void Save()
        {
            AssetDatabase.CreateAsset(this,DIR.Value + FILE_NAME);
            AssetDatabase.Refresh();
        }

        private static readonly Lazy<string> DIR = new Lazy<string>(() =>
        {
            var dir = "Assets/QFramework/Settings/ViewControllerKit/";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        });
        
        private const string FILE_NAME = "Setting.asset";
    }
}