using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AssetBundleMaster.Common
{
    /// <summary>
    /// Global varables in plugin
    /// </summary>
    public static class GlobalVariable
    {
        public static bool isRuntimeMode
        {
            get
            {
#if UNITY_EDITOR
                return UnityEngine.Application.isPlaying;
#else
                return true;
#endif
            }
        }
    }

    public class IDGen
    {
        private static int _ID = 0;
        public static int GetNewID
        {
            get
            {
                return _ID++;
            }
        }

        private int _id = 0;
        public int getNewID
        {
            get
            {
                return _id++;
            }
        }
    }

#if UNITY_EDITOR
    public static class Editor
    {
        public static class Application
        {
            // base of project, the Application path cant be use in editor Ctor
            public static readonly FixedStringData projectPath = new FixedStringData(() => { return LeftSlash(System.Environment.CurrentDirectory); });
            public static readonly FixedStringData dataPath = new FixedStringData(() => { return LeftSlash(System.IO.Path.Combine(projectPath, "Assets")); });
            public static readonly FixedStringData streamingAssetsPath = new FixedStringData(() => { return LeftSlash(System.IO.Path.Combine(dataPath, "StreamingAssets")); });

            public const string Resources = "Assets/Resources";
            public const string AssetBundleMasterEditorPath = "Assets/Editor/AssetBundleMaster";

            public static string AssetBundleMasterEditorConfigPath
            {
                get
                {
                    return AssetBundleMasterEditorPath + "/EditorConfigs";
                }
            }

            public static string LeftSlash(string str)
            {
                if(string.IsNullOrEmpty(str) == false)
                {
                    return str.Replace("\\", "/");
                }
                return str;
            }
        }
    }

    public class FixedStringData
    {
        public bool alwaysGet = true;
        private System.Func<string> _valueFunc = null;
        private bool _dataRead = false;
        public string data { get; private set; }

        public FixedStringData(System.Func<string> valueFunc, bool alwaysGet = false)
        {
            _valueFunc = valueFunc;
            this.alwaysGet = alwaysGet;
        }

        public static implicit operator string(FixedStringData data)
        {
            if(data.alwaysGet || false == data._dataRead)
            {
                data._dataRead = true;
                data.data = data._valueFunc.Invoke();
            }
            return data.data;
        }
    }
#endif
}

