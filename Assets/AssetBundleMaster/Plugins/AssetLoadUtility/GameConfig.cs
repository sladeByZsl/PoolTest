using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.Common;

    /// <summary>
    /// the game base config -- will follow the build app
    /// game config is set with <string, string> -- combine with loadpath-loadAttributes
    /// </summary>
    public class GameConfig : Singleton<GameConfig>
    {
        public const string BundleDefaultExtName = ".ab";
        public const string MetaFileExtName = ".meta";
        public const string AtlasHeader = "atlas";
        public const string AssetBundleManifestName = "AssetBundleManifest";
        public const string ConfigFileFullName = "AssetBundleMaster/GameConfig";
        public const string ResourcesLoadModeRuntime = "ResourcesLoadModeRuntime";
        public const string ResourcesLoadModeEditor = "ResourcesLoadModeEditor";
        public const string SpriteAtlasFolderEditor = "Assets/AssetBundleMasterSpriteAtlas";
        public const string Version = "Version";
        public const ResourcesLoadMode DefauLoadMode = ResourcesLoadMode.Resources;

        // path caches
        public static PreCacheData<string> CurrentDirectory = new PreCacheData<string>(() => { return Utility.LeftSlash(System.Environment.CurrentDirectory); });
        public static PreCacheData<string> dataPath = new PreCacheData<string>(() => { return Utility.LeftSlash(Application.dataPath); });
        public static PreCacheData<string> streamingAssetsPath = new PreCacheData<string>(() => { return Utility.LeftSlash(Application.streamingAssetsPath); });
        public static PreCacheData<string> persistentDataPath = new PreCacheData<string>(() => { return Utility.LeftSlash(Application.persistentDataPath); });
        public static PreCacheData<string> temporaryCachePath = new PreCacheData<string>(() => { return Utility.LeftSlash(Application.temporaryCachePath); });

#if UNITY_EDITOR
        public const string AssetBundlesOutputPath = "/AssetBundles";

        public static string DefaultBuildRoot { get { return "Assets/AssetBundleMaster/AssetBundleMasterExample"; } }

        public static PlayerPrefsData<string> currentVersion = new PlayerPrefsData<string>("AssetVersion", "1.0.0");

        public static PlayerPrefsData<BuildTarget> currentPlatform = new PlayerPrefsData<BuildTarget>("PlatformVersion", () =>
        { return EditorUserBuildSettings.activeBuildTarget; }).Set(deserializeFunc: (_str) =>
        { return (BuildTarget)System.Enum.Parse(typeof(BuildTarget), _str); });

        public static string getPlatformFolder { get { return string.Concat(CurrentDirectory.data, AssetBundlesOutputPath, "/", currentPlatform.data.ToString()); } }    // dont cache

        public static string getPlatformVersionFolder { get { return string.Concat(getPlatformFolder, "/", currentVersion.data); } }

        public static string RuntimeConfigAssetSavePath { get { return string.Concat(Editor.Application.Resources, "/", ConfigFileFullName, ".asset"); } }
#endif

        // the detail of game gameConfigSetting -- runtime
        public GameConfigSetting gameConfigSetting { get; private set; }
        public ResourcesLoadMode resourceLoadMode
        {
            get
            {
#if UNITY_EDITOR
                return gameConfigSetting.ResourcesLoadModeEditor;
#else
                return gameConfigSetting.ResourcesLoadModeRuntime;
#endif
            }
        }
        public System.Version version
        {
            get
            {
                return gameConfigSetting.version;
            }
        }
        public string assetBuildRoot
        {
            get
            {
                return gameConfigSetting.assetBuildRoot;
            }
#if UNITY_EDITOR
            set
            {
                gameConfigSetting.assetBuildRoot = value;
                ConfigSave();
            }
#endif
        }

        public bool isEditorMode
        {
#if UNITY_EDITOR
            get { return resourceLoadMode == ResourcesLoadMode.AssetDataBase_Editor; }
#else
            get { return false; }
#endif
        }
        public bool isBundleMode
        {
            get
            {
                switch(resourceLoadMode)
                {
#if UNITY_EDITOR
                    case ResourcesLoadMode.AssetBundle_EditorTest:
#endif
                    case ResourcesLoadMode.AssetBundle_StreamingAssets:
                    case ResourcesLoadMode.AssetBundle_PersistentDataPath:
                    case ResourcesLoadMode.AssetBundle_Remote:
                        {
                            return true;
                        }
                    default:
                        {
                            return false;
                        }
                }
            }
        }
        public bool isResourcesMode
        {
            get { return resourceLoadMode == ResourcesLoadMode.Resources; }
        }
        public bool isRemoteAssets
        {
            get { return resourceLoadMode == ResourcesLoadMode.AssetBundle_Remote; }
        }


        #region Override Funcs
        protected override void Initialize()
        {
#if UNITY_EDITOR
            RequireGameConfigSetting();
#endif
            // runtime
            gameConfigSetting = LoadGameConfigSetting(ConfigFileFullName);
        }

        protected override void UnInitialize()
        {
            gameConfigSetting = null;
        }
        #endregion

        #region Help Funcs
        private GameConfigSetting CreateDefaultConfig()
        {
            var config = GameConfigSetting.CreateInstance<GameConfigSetting>();
            config.ResourcesLoadModeRuntime = DefauLoadMode;
#if UNITY_EDITOR
            config.ResourcesLoadModeEditor = ResourcesLoadMode.AssetDataBase_Editor;
            config.assetBuildRoot = DefaultBuildRoot;
#endif
            config.Version = "1.0.0";
            return config;
        }
        // load config setting
        private GameConfigSetting LoadGameConfigSetting(string loadPath)
        {
            var config = Resources.Load<GameConfigSetting>(loadPath);
            if(config == false)
            {
                config = CreateDefaultConfig();
            }
            return config;
        }

#if UNITY_EDITOR
        // require the gameconfig setting file
        private void RequireGameConfigSetting()
        {
            gameConfigSetting = AssetDatabase.LoadAssetAtPath<GameConfigSetting>(RuntimeConfigAssetSavePath);
            if(gameConfigSetting == false)
            {
                // create new data
                gameConfigSetting = CreateDefaultConfig();
                ConfigSave();
            }
        }

        // set field key value
        public void SetKeyValue(string key, object value)
        {
            if(string.IsNullOrEmpty(key) == false)
            {
                SetField(gameConfigSetting, key, value);
            }
        }

        // save config to file
        public void ConfigSave()
        {
            GameConfigSetting.SaveToAsset<GameConfigSetting>(gameConfigSetting, RuntimeConfigAssetSavePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // full path to Asset/... path -- leftslash allready
        public static string FullPathToProjectPath(string fullName)
        {
            if(!string.IsNullOrEmpty(fullName))
            {
                fullName = (fullName).Replace("\\", "/");
                string headerName = (System.Environment.CurrentDirectory).Replace("\\", "/");
                fullName = fullName.Replace(headerName, "");
                while(fullName.StartsWith("/"))
                {
                    fullName = fullName.Substring(1);
                }
                return fullName;
            }
            return string.Empty;
        }

        // editor load root
        public string ShowLoadRootPath()
        {
            string loadRoot = "";
            switch(resourceLoadMode)
            {
                case ResourcesLoadMode.Resources:
                    { loadRoot = "Resources"; } // Resources mode has no unique file path
                    break;
                case ResourcesLoadMode.AssetBundle_StreamingAssets:
                    { loadRoot = AssetBundleMaster.Common.Editor.Application.streamingAssetsPath; }
                    break;
                case ResourcesLoadMode.AssetBundle_PersistentDataPath:
                    { loadRoot = Application.persistentDataPath; }
                    break;
                case ResourcesLoadMode.AssetBundle_Remote:
                    { loadRoot = gameConfigSetting.RemoteURL; }
                    break;
                case ResourcesLoadMode.AssetDataBase_Editor:
                    { loadRoot = assetBuildRoot; }
                    break;
                case ResourcesLoadMode.AssetBundle_EditorTest:
                    { loadRoot = getPlatformVersionFolder; }
                    break;
            }
            return loadRoot;
        }

        // reflection set field
        public static void SetField(object ins, string fieldName, object val)
        {
            var curType = ins.GetType();
            FieldInfo tagField = null;
            do
            {
                tagField = curType.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.GetField | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField);
                curType = curType.BaseType;
            } while(tagField == null && curType != null);

            if(tagField != null)
            {
                tagField.SetValue(ins, val);
            }
        }
#endif
        #endregion
    }

}