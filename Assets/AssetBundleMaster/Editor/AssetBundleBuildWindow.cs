using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetBundleMaster.Editor
{
    using AssetBundleMaster.ContainerUtilities;
    using AssetBundleMaster.AssetLoad;
    using AssetBundleMaster.Extention;
    using AssetBundleMaster.Common;

    /// <summary>
    /// Serialized ScriptableObjects :
    ///     GameConfig
    ///     
    /// Serialized Json Files :
    ///     BundleConfig
    ///     ShaderConfig
    ///     
    /// Serialized PlayerPrefs : 
    ///     AssetBundleMasterPlayerPrefsDatas
    /// </summary>
    public class AssetBundleBuildWindow : MyEditorWindow<AssetBundleBuildWindow>
    {
        [System.Serializable]
        public class BundleConfigInfo
        {
            public string Folder = string.Empty;
            public string BundleNameSet = string.Empty;
            public bool TopDirectoryOnly = true;

            public void ResetFolder(string folderPath)
            {
                var projectPath = CommonEditorUtils.FullPathToProjectPath(folderPath);
                Folder = projectPath;
            }

            public bool Applyable(string assetPath)
            {
                if(string.IsNullOrEmpty(assetPath) == false && string.IsNullOrEmpty(Folder) == false && string.IsNullOrEmpty(BundleNameSet) == false)
                {
                    var assetDir = Utility.GetDirectoryName(assetPath);
                    if(TopDirectoryOnly)
                    {
                        return string.Equals(assetDir, Folder, StringComparison.Ordinal);
                    }
                    else
                    {
                        return assetDir.StartsWith(Folder, StringComparison.Ordinal);
                    }
                }
                return false;
            }
        }
        [System.Serializable]
        public class ShaderConfigInfo
        {
            public bool collect = false;
            public Shader shader { get; set; }
        }

        // vars
        const string GraphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";
        const string g_bundleNameConfigFile = "BundleConfig.json";
        const string g_builtinShaderConfigFile = "ShaderConfig.json";
        const string g_localversionFile = "VersionInfo.prefab";
        const string g_manifestExt = ".manifest";
        const string g_spriteAtlasExt = ".spriteatlas";
        const string g_atlasHeader = "atlas/";
        const string g_shaderCollectionFolder = "shadercollection/";
        const string g_exampleFolderName = "AssetBundleMasterExample";
        const string g_sceneExt = ".unity";

        const string spriteFormat = "    - {fileID: 2800000, guid: #KEY#, type: 3}";    // this is based on Unity Version maybe

        // VersionInfo path
        public static FixedStringData versionInfoLoadPathToBundleInfo
            = new FixedStringData(() => { return CommonEditorUtils.LeftSlash(Path.Combine(GameConfig.Instance.assetBuildRoot, g_localversionFile)); }, true);
        // shader settings
        public static FixedStringData builtinShaderConfigFilePath
            = new FixedStringData(() => { return CommonEditorUtils.LeftSlash(Path.Combine(Common.Editor.Application.AssetBundleMasterEditorConfigPath, g_builtinShaderConfigFile)); });

        // build version control
        static System.Version _buildVersion = null;
        static System.Version buildVersion
        {
            get
            {
                if(_buildVersion == null)
                {
                    var versionStr = GameConfig.currentVersion.data;
                    _buildVersion = new System.Version(versionStr);
                }
                return _buildVersion;
            }
            set
            {
                try
                {
                    var versionStr = value.ToString(3);
                    _buildVersion = value;
                    GameConfig.currentVersion.data = versionStr;
                    SetGameConfig(GameConfig.Version, versionStr, true);
                }
                catch { }
            }
        }
        static string buildVersionStr
        {
            get
            {
                try
                {
                    var versionStr = buildVersion.ToString(3);
                    return versionStr;
                }
                catch
                {
                    return "1.0.0";
                }
            }
        }
        static List<string> ms_allVersions = new List<string>();    // all versions in cur folder

        // Load Mode Control
        private ResourcesLoadMode editorResourceLoadMode
        {
            get
            {
                return GameConfig.Instance.gameConfigSetting.ResourcesLoadModeEditor;
            }
            set
            {
                SetGameConfig(GameConfig.ResourcesLoadModeEditor, value, true);
            }
        }
        private ResourcesLoadMode runtimeResourceLoadMode
        {
            get
            {
                return GameConfig.Instance.gameConfigSetting.ResourcesLoadModeRuntime;
            }
            set
            {
                SetGameConfig(GameConfig.ResourcesLoadModeRuntime, value, true);
            }
        }
        // BuildOptoin Control
        private PlayerPrefsData<BuildAssetBundleOptions> m_buildOpt
            = new PlayerPrefsData<BuildAssetBundleOptions>("BuildAssetBundleOptions", BuildAssetBundleOptions.ChunkBasedCompression).Set(serializeFunc: (_opt) =>
            {
                return ((int)_opt).ToString();
            }, deserializeFunc: (_str) =>
            {
                int val = 0;
                int.TryParse(_str, out val);
                return (BuildAssetBundleOptions)val;
            });
        // should build update file info?
        private PlayerPrefsData<bool> m_buildUpdateInfo = new PlayerPrefsData<bool>("BuildUpdateInfo", false);
        // should clear built temp files?
        private PlayerPrefsData<bool> m_clearBuildTempFile = new PlayerPrefsData<bool>("ClearBuildTempFile", false);

        // loop reference object check func
        private PlayerPrefsData<JsonUtilitySerializationDictionary<string, ShaderConfigInfo>> m_builtinShaderConfigCache
          = new PlayerPrefsData<JsonUtilitySerializationDictionary<string, ShaderConfigInfo>>(builtinShaderConfigFilePath).Set((_dict) =>
          {
              return _dict.ToJson();
          }, (_json) =>
          {
              return JsonUtilitySerializationDictionary<string, ShaderConfigInfo>.FromJson(_json);
          }, (_loadPath) =>
          {
              if(System.IO.File.Exists(_loadPath) == false)
              {
                  return string.Empty;
              }
              return System.IO.File.ReadAllText(_loadPath);
          }, (_str) =>
          {
              FileAccessWrappedFunction.WriteStringToFile(builtinShaderConfigFilePath, _str, false);
          });

        private static List<ShaderConfigInfo> ms_collectBuiltinShaders = new List<ShaderConfigInfo>();

        #region UI Controls
        private Vector2 m_existsVersionScroll = Vector2.zero;
        private Vector2 m_builtinShaderScroll = Vector2.zero;
        private Vector2 m_collectedBuiltinShaderScroll = Vector2.zero;

        private static readonly string[] ms_runtimeLoadModes = new string[4]
        {
            ResourcesLoadMode.Resources.ToString(),
            ResourcesLoadMode.AssetBundle_StreamingAssets.ToString(),
            ResourcesLoadMode.AssetBundle_PersistentDataPath.ToString(),
            ResourcesLoadMode.AssetBundle_Remote.ToString(),
        };
        private static string[] ms_runtimeLoadModesShow = null;
        private static int[] ms_runtimeLoadModesShowValue = null;
        private static Shader ms_tempTargetedShader = null;

        // EnumMaskField is Incorrect in Unity5, fix by manuaul
        private static string[] _buildOpts = null;
        private static string[] ms_buildOpts
        {
            get
            {
                if(_buildOpts == null)
                {
                    // Incorrect in Unity5
                    var optNames = System.Enum.GetNames(typeof(BuildAssetBundleOptions));
                    _buildOpts = new string[optNames.Length - 1];
                    for(int i = 0; i < _buildOpts.Length; i++)
                    {
                        _buildOpts[i] = optNames[i + 1];
                    }
                }
                return _buildOpts;
            }
        }
        #endregion


        // show setting window
        [MenuItem("Tools/AssetBundleMaster/AssetBundleMaster Window", false, 100)]
        public static void WindowShow()
        {
            Init();
        }

        [MenuItem("Tools/AssetBundleMaster/Others/Clear AssetBundleMaster Generated Datas", false, 150)]
        public static void ClearGenerated()
        {
            if(CommonEditorUtils.MessageBox("Clear AssetBundleMaster Generated Datas ?"))
            {
                PlayerPrefsDataKeys.DeleteAllKeys();
                CommonEditorUtils.DeleteAsset(Resources.Load<GameConfigSetting>(GameConfig.ConfigFileFullName));
                AssetDatabase.DeleteAsset(GameConfig.SpriteAtlasFolderEditor);
                AssetDatabase.DeleteAsset(AssetBundleMaster.Common.Editor.Application.AssetBundleMasterEditorPath);
                AssetDatabase.DeleteAsset(AssetBundleMaster.Common.Editor.Application.Resources + "/AssetBundleMaster");
                var versionInfos = System.IO.Directory.GetFiles(Application.dataPath, "VersionInfo.prefab", SearchOption.AllDirectories);
                if(versionInfos != null && versionInfos.Length > 0)
                {
                    foreach(var versionInfo in versionInfos)
                    {
                        AssetDatabase.DeleteAsset(CommonEditorUtils.FullPathToProjectPath(versionInfo));
                    }
                }
                CommonEditorUtils.SaveAndRefresh(ImportAssetOptions.Default);
            }
        }

        [MenuItem("Tools/AssetBundleMaster/Others/Open Caching Folder", false, 200)]
        public static void ShowCacheFolder()
        {
#if UNITY_2017_1_OR_NEWER
            System.Diagnostics.Process.Start(Caching.currentCacheForWriting.path);
#else
            System.Diagnostics.Process.Start(Application.temporaryCachePath);
#endif
        }

        [MenuItem("Tools/AssetBundleMaster/Copy built assetbundles From TempFolder To StreamingAssets", false, 350)]
        public static void CopyTempFolderAssetBundlesToStreamingAssets()
        {
            var tips = "Copy " + GameConfig.getPlatformVersionFolder + " To StreamingAssets Folder ?";
            if(CommonEditorUtils.MessageBox(tips))
            {
                var buildedManifestPath = GameConfig.getPlatformVersionFolder + "/" + "AssetBundleManifest";
                var buildedManifest = AssetBundle.LoadFromFile(buildedManifestPath);
                if(buildedManifest)
                {
                    try
                    {
                        if(CommonEditorUtils.MessageBox("Do Copy AssetBundes ?\nFrom: "
                            + GameConfig.getPlatformVersionFolder + "\nTo : " + Application.streamingAssetsPath))
                        {
                            var tagFolder = Application.streamingAssetsPath;
                            CopyBuiltAssetFiles(GameConfig.getPlatformVersionFolder,
                                buildedManifest.LoadAsset<AssetBundleManifest>("AssetBundleManifest"),
                                tagFolder, (_done, _total) =>
                                {
                                    EditorUtility.DisplayProgressBar("Copying", "Files : " + _done + "/" + _total, _done / _total);
                                }, () =>
                                {
                                    EditorUtility.ClearProgressBar();
                                    Debug.Log("Copied To Folder : " + tagFolder);
                                    System.Diagnostics.Process.Start(tagFolder);
                                    CommonEditorUtils.SaveAndRefresh(ImportAssetOptions.Default);
                                });
                        }
                    }
                    finally
                    {
                        buildedManifest.Unload(false);
                    }
                }
            }
        }

        public AssetBundleBuildWindow()
        {
            ReadConfig();
        }

        protected override void Reload()
        {
            this.minSize = new Vector2(1000, 800);
            Clear();
            ReadConfig();

            EditorConfigSettings();
        }

        #region Main Funcs
        /// <summary>
        /// Clear all assetbundles and history list
        /// </summary>
        /// <param name="progress"></param>
        public bool AutoClearAllAssetBundleNames()
        {
            this.ShowNotification(new GUIContent("Clearing !!!"));

            bool succ = true;
            var allAssetBundles = AssetDatabase.GetAllAssetBundleNames();
            if(allAssetBundles != null && allAssetBundles.Length > 0)
            {
                float done = 0;
                float totalCount = allAssetBundles.Length;
                float countInv = 1.0f / totalCount;
                for(int i = 0; i < totalCount; i++)
                {
                    done++;
                    var progress = done * countInv;
                    string info = "Cleaning... [ " + done + " / " + totalCount + " ] " + (progress * 100.0f).ToString("F2") + "%";
                    if(EditorUtility.DisplayCancelableProgressBar("Cleaning AssetBundleNames", info, progress))
                    {
                        succ = false;
                        break;
                    }

                    string assetBundleName = allAssetBundles[i];
                    var assets = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
                    if(assets != null && assets.Length > 0)
                    {
                        foreach(var asset in assets)
                        {
                            var assetImporter = AssetImporter.GetAtPath(asset);
                            if(assetImporter)
                            {
                                assetImporter.assetBundleName = null;
                            }
                        }
                    }
                }
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.RemoveUnusedAssetBundleNames();
            CommonEditorUtils.SaveAndRefresh(ImportAssetOptions.Default);
            return succ;
        }
        /// <summary>
        /// Auto set runtime assetbundles
        /// </summary>
        /// <param name="baseFolder"></param>
        /// <param name="progress"></param>
        /// <param name="titles"></param>
        private bool AutoSetRunTimeResources(string baseFolder, VersionInfo localVersionFolder)
        {
            CommonEditorUtils.ObsoluteTime timer = new CommonEditorUtils.ObsoluteTime();
            timer.Start();

            // temp assets
            HashSet<string> mainAssets = new HashSet<string>();
            Dictionary<string, HashSet<string>> spriteAtlasAssets = new Dictionary<string, HashSet<string>>();

            // get all main assets
            if(System.IO.Directory.Exists(baseFolder))
            {
                EditorUtility.DisplayProgressBar("AutoSet AssetBundles", "Scanning Main Assets...", 0f);
                var files = Directory.GetFiles(baseFolder, "*.*", SearchOption.AllDirectories);
                if(files != null && files.Length > 0)
                {
                    var aviableFiles = files.Where(_path => CheckIsResources(_path)).ToList();
                    if(aviableFiles.Count > 0)
                    {
                        var done = 0.0f;
                        float totalCount = aviableFiles.Count;
                        float totalInv = 1.0f / aviableFiles.Count;
                        for(int i = 0, imax = aviableFiles.Count; i < imax; i++)
                        {
                            var loadPath = aviableFiles[i];
                            mainAssets.Add(CommonEditorUtils.FullPathToProjectPath(loadPath));
                            done++;
                            float progress = done * totalInv;
                            string info = string.Concat("Scanning Main Assets... [ ", done, " / ", totalCount, " ] ", (progress * 100.0f).ToString("F2"), "%");
                            if(EditorUtility.DisplayCancelableProgressBar("AutoSet AssetBundles", info, progress))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            Debug.Log("Collection Take Time : " + timer.GetSeconds() + " / " + mainAssets.Count);

            // referenceAsset controller
            ReferenceAssetProccess referenceAssetProccess = new ReferenceAssetProccess(baseFolder, (_assetLoadPath, _baseFolder) =>
            {
                WrapSetAssetBundleName(_assetLoadPath, _baseFolder, true, spriteAtlasAssets, GameConfig.BundleDefaultExtName, isInFolder: false);
            }, mainAssets);
            referenceAssetProccess.conflict = (_src, _ref) =>
            {
#if UNITY_2018_1_OR_NEWER
                if(_src.EndsWith(".prefab") && PrefabUtility.GetPrefabAssetType(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_ref)) == PrefabAssetType.Model)
                {
                    return true;
                }
#else
                if(_src.EndsWith(".prefab") && PrefabUtility.GetPrefabType(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_ref)) == PrefabType.ModelPrefab)
                {
                    return true;
                }
#endif
                return false;
            };

            // set main files
            var baseFolderPath = CommonEditorUtils.FullPathToProjectPath(baseFolder);
            if(mainAssets.Count > 0)
            {
                var done = 0.0f;
                float totalCount = mainAssets.Count;
                float totalInv = 1.0f / mainAssets.Count;
                foreach(var mainAsset in mainAssets)
                {
                    referenceAssetProccess.Proccess(mainAsset);

                    var assetImporter = WrapSetAssetBundleName(mainAsset, baseFolder, true, spriteAtlasAssets, GameConfig.BundleDefaultExtName, isInFolder: true);
                    if(assetImporter)
                    {
                        var assetBundleName = assetImporter.assetBundleName;
                        //var assetBundleName = GetAssetBundleFullName(assetImporter);  // no need to use variant
                        // set main asset load info to local version
                        SetToLocalVersion(localVersionFolder, mainAsset, baseFolderPath, assetBundleName);
                        /*
                         * Because we do set assetbundle name automaticaly, the final built asset path may changed, so we must log origin load path.
                         * for example:
                         *      asset A is in folder AAA -> AAA/A.png
                         *      asset B is in folder BBB -> BBB/B.png
                         *      but the built asset bundle is not the same as it was in editor, may be like this : 
                         *      bbb/a.ab
                         *      aaa/b.ab
                         * So the LoadAll will load error if use System.IO.Directory.GetFiles(), and Android will not allow this API in StreamingAssets
                         */
                    }

                    done++;
                    float progress = done * totalInv;
                    string info = string.Concat("Set Main Assets... [ ", done, " / ", totalCount, " ] ", (progress * 100.0f).ToString("F2"), "%");
                    if(EditorUtility.DisplayCancelableProgressBar("AutoSet AssetBundles", info, progress))
                    {
                        return false;
                    }
                }
            }

            Debug.Log("Main Take Time : " + timer.GetSeconds() + " / " + mainAssets.Count);

            if(m_builtinShaderConfigCache.data.data.Count > 0)
            {
                referenceAssetProccess.materialProccess = new ReferenceAssetProccess.MaterialProcess(ProcessMaterial);
            }
            // set sub assets
            referenceAssetProccess.SetAssetBundleNames((_done, _totalCount, _progress) =>
            {
                string info = string.Concat("Set Sub Assets... [ ", _done, " / ", _totalCount, " ] ", (_progress * 100.0f).ToString("F2"), "%");
                return EditorUtility.DisplayCancelableProgressBar("AutoSet AssetBundles", info, _progress) == false;
            });

            Debug.Log("Conficted Count : " + referenceAssetProccess.confictedCount);

            var time = timer.GetSeconds();
            Debug.Log("Set AssetBundle Names Take Time : " + time + " / " + referenceAssetProccess.refCounter.Count);

#if UNITY_2017_1_OR_NEWER
            CreateSpriteAtlasAssets(localVersionFolder, spriteAtlasAssets);
#endif
            return true;
        }
        /// <summary>
        /// build bundles
        /// </summary>
        public void BuildAssetBundles(bool autoProcess = false)
        {
            this.ShowNotification(new GUIContent("Building ... "));

            // Choose the output path according to the build target. -- add version folder
            string outputPath = GameConfig.getPlatformVersionFolder;
            string versionStr = buildVersionStr;
            bool copied = false;

            CommonEditorUtils.RequireDirectory(outputPath);

            var existsBuild = LoadFromBundleFile<AssetBundleManifest>(outputPath + "/" + GameConfig.AssetBundleManifestName, GameConfig.AssetBundleManifestName);
            if(existsBuild == false)
            {
                var currentVersion = versionStr;
                bool hasVersion = ms_allVersions.IndexOf(currentVersion) >= 0;

                if(hasVersion == false)
                {
                    int lowerIndex = ms_allVersions.Count - 1;
                    for(int i = 0; i < ms_allVersions.Count; i++)
                    {
                        var compareVersion = new System.Version(ms_allVersions[i]);
                        if(compareVersion > buildVersion)
                        {
                            lowerIndex = i - 1;
                            break;
                        }
                    }
                    if(lowerIndex >= 0)
                    {
                        var lastVersion = ms_allVersions[lowerIndex];
                        if(autoProcess || UnityEditor.EditorUtility.DisplayDialog("Creating New Version ...",
                           string.Format("    To build new version may take a long time, \n use latest version ({0}) as manifest (build faster) ?", lastVersion), "OK", "Do Not Use It"))
                        {
                            var lastVersionFolder = GameConfig.getPlatformFolder + "/" + lastVersion;
                            DirectoryCopy(lastVersionFolder, outputPath, true);
                            copied = true;
                        }
                    }
                    if(ms_allVersions.Contains(currentVersion) == false)
                    {
                        ms_allVersions.Add(currentVersion);
                    }
                    AllVerisonSort();
                }
            }

            //@TODO: build... (Make sure pipeline works correctly with it.)
            AssetBundleManifest buildedManifest = null;
            try
            {
                buildedManifest = BuildPipeline.BuildAssetBundles(outputPath, m_buildOpt.data, GameConfig.currentPlatform.data);
                if(buildedManifest)
                {
                    var di = new DirectoryInfo(outputPath);

                    // rename manifest files
                    FileInfo manifestFI = new FileInfo(outputPath + "/" + versionStr);
                    FileAccessWrappedFunction.ReNameFile(manifestFI, di, GameConfig.AssetBundleManifestName);

                    FileInfo manifest = new FileInfo(outputPath + "/" + versionStr + g_manifestExt);
                    FileAccessWrappedFunction.ReNameFile(manifest, di, GameConfig.AssetBundleManifestName + g_manifestExt);

                    if((autoProcess && (existsBuild || copied)) || m_clearBuildTempFile.data)
                    {
                        TrimBuiltAssetFiles(outputPath, buildedManifest);   // trim by manifest in case has non-versioned file
                    }

                    // update infos
                    if(m_buildUpdateInfo.data)
                    {
                        var updates = GetUpdateInfos(buildedManifest);
                        if(updates != null)
                        {
                            foreach(var update in updates)
                            {
                                string saveUpdateFilePath = string.Format("{0}/Update_{1}_to_{2}.txt", GameConfig.getPlatformFolder, update.from, update.to);
                                SaveUpdateFile(saveUpdateFilePath, update);
                            }
                        }
                    }

                    if(runtimeResourceLoadMode == ResourcesLoadMode.AssetBundle_StreamingAssets
                        || editorResourceLoadMode == ResourcesLoadMode.AssetBundle_StreamingAssets)
                    {
                        // copy to streaming assets folder
                        if(autoProcess || CommonEditorUtils.MessageBox("Copy To StreamingAssets Folder ?"))
                        {
                            string tagFolder = Application.streamingAssetsPath;
                            CopyBuiltAssetFiles(outputPath, buildedManifest, tagFolder, (_done, _total) =>
                            {
                                EditorUtility.DisplayProgressBar("Copying", "Files : " + _done + "/" + _total, _done / _total);
                            }, () =>
                            {
                                EditorUtility.ClearProgressBar();
                                Debug.Log("AssetBundles Copied To Folder : " + tagFolder);
                                System.Diagnostics.Process.Start(tagFolder);
                            });
                        }
                    }
                }
            }
            catch(System.Exception ex)
            {
                Debug.LogError(@ex.ToString());
            }

            if(buildedManifest)
            {
                this.ShowNotification(new GUIContent("Build AssetBundle Successed !!"));
                // open folder
                string path = CommonEditorUtils.LeftSlash(GameConfig.getPlatformVersionFolder);
                System.Diagnostics.Process.Start(path);
                CommonEditorUtils.SaveAndRefresh(ImportAssetOptions.Default);
            }
            else
            {
                this.ShowNotification(new GUIContent("Build AssetBundle FAILED !!"));
            }
        }

        private void StepAll()
        {
            if(CommonEditorUtils.MessageBox("Start All Steps ?"))
            {
                this.ShowNotification(new GUIContent("Start"));

                EditorApplication.delayCall += () =>
                {
                    if(AutoClearAllAssetBundleNames())
                    {
                        WrappedProccessAssetBundleName((_baseFolder, _localVersionFolder) =>
                        {
                            return this.AutoSetRunTimeResources(_baseFolder, _localVersionFolder);
                        }, () =>
                        {
                            BuildAssetBundles(true);
                            this.ShowNotification(new GUIContent("End"));
                        });
                    }
                    else
                    {
                        this.ShowNotification(new GUIContent("Clear Cacncelled !!!"));
                    }
                };
            }
        }

        private void ReadConfig()
        {
            ms_runtimeLoadModesShow = System.Enum.GetNames(typeof(ResourcesLoadMode));
            ms_runtimeLoadModesShowValue = new int[ms_runtimeLoadModesShow.Length];
            for(int i = 0; i < ms_runtimeLoadModesShow.Length; i++)
            {
                if(ms_runtimeLoadModes.Contains(ms_runtimeLoadModesShow[i]) == false)
                {
                    ms_runtimeLoadModesShow[i] = "[Not Support] -- " + ms_runtimeLoadModesShow[i];
                }
                ms_runtimeLoadModesShowValue[i] = i;
            }
        }
        private static void SaveConfig()
        {
            GameConfig.Instance.ConfigSave();
        }
        private void Clear()
        {
            GameConfig.Destroy();
        }
        #endregion

        #region Mono Funcs
        void OnEnable()
        {
            CheckAllVersions();
            var builtinShaders = GetBuiltInShaders();
            ms_collectBuiltinShaders = new List<ShaderConfigInfo>();
            foreach(var builtinShader in builtinShaders)
            {
                var data = m_builtinShaderConfigCache.data.data.TryGetValue(builtinShader.name);
                bool collect = data != null ? data.collect : false;
                ms_collectBuiltinShaders.Add(new ShaderConfigInfo() { collect = collect, shader = builtinShader });
            }
            ms_collectBuiltinShaders.Sort((_l, _r) =>
            {
                return Comparer<string>.Default.Compare(_l.shader.name, _r.shader.name);
            });
        }

        void OnGUI()
        {
            CommonEditorUtils.VerticalLayout(() =>
            {
                GUILayout.Space(20.0f);
                CommonEditorUtils.DrawLine("Editor Resources LoadMode");
                var editorLoadModeTips = (new GUIContent("Editor Asset LoadMode : ", "Play in Editor, this mode is used"));
                var oldLoadMode = editorResourceLoadMode;
                var newLoadMode = (ResourcesLoadMode)EditorGUILayout.EnumPopup(editorLoadModeTips, oldLoadMode, GUILayout.Width(500.0f));
                if(newLoadMode != oldLoadMode)
                {
                    editorResourceLoadMode = newLoadMode;
                }
                RemoteURL_GUI(() => { return editorResourceLoadMode == ResourcesLoadMode.AssetBundle_Remote; });

                GUILayout.Space(20.0f);
                CommonEditorUtils.DrawLine("Runtime Resources LoadMode");
                var oldruntimeLoadMode = runtimeResourceLoadMode.ToString();
                int oldIndex = ms_runtimeLoadModesShow.ArrayIndexOf(oldruntimeLoadMode);
                var newIndex = EditorGUILayout.IntPopup("Runtime Asset LoadMode : ", oldIndex, ms_runtimeLoadModesShow, ms_runtimeLoadModesShowValue, GUILayout.Width(500.0f));
                if(newIndex != oldIndex)
                {
                    if(ms_runtimeLoadModes.Contains(ms_runtimeLoadModesShow[newIndex]))
                    {
                        try
                        {
                            runtimeResourceLoadMode = (ResourcesLoadMode)System.Enum.Parse(typeof(ResourcesLoadMode), ms_runtimeLoadModesShow[newIndex]);
                        }
                        catch(System.Exception ex)
                        {
                            Debug.LogError(@ex.ToString());
                        }
                    }
                }
                RemoteURL_GUI(() => { return runtimeResourceLoadMode == ResourcesLoadMode.AssetBundle_Remote; });

                GUILayout.Space(20.0f);
                CommonEditorUtils.DrawLine("Platform Settings");
                CommonEditorUtils.VerticalLayout(() =>
                {
                    var lastPlatform = GameConfig.currentPlatform.data;
                    var newPlatform = (BuildTarget)EditorGUILayout.EnumPopup("Platform Selection : ", lastPlatform);

                    if(GUILayout.Button("Set Current Platform", GUILayout.Height(25.0f)))
                    {
                        newPlatform = EditorUserBuildSettings.activeBuildTarget;
                    }

                    if(newPlatform != lastPlatform)
                    {
                        GameConfig.currentPlatform.data = newPlatform;
                        CheckAllVersions();
                    }
                }, 500.0f);

                GUILayout.Space(10.0f);
                CommonEditorUtils.DrawLine("Version Settings");
                CommonEditorUtils.VerticalLayout(() =>
                {
                    CommonEditorUtils.HorizontalLayout(() =>
                    {
                        var curVersion = buildVersionStr;
                        GUILayout.Label("Current Version : " + curVersion);
                        var newVersionStr = EditorGUILayout.TextField("Set Bundle Version : ", curVersion);
                        if(newVersionStr != curVersion)
                        {
                            VersionChanged(newVersionStr);
                        }
                    });

                    GUILayout.Space(10.0f);

                    CommonEditorUtils.HorizontalLayout(() =>
                    {
                        GUILayout.Label("Exists Versions : ");
                        CommonEditorUtils.VerticalLayout(() =>
                        {
                            CommonEditorUtils.ScrollViewLayout(ref m_existsVersionScroll, () =>
                            {
                                VersionsGUI();
                            }, 200.0f, 100.0f);
                        });
                    });
                }, 500.0f);

                GUILayout.Space(10.0f);
                GUILayout.Label("-------------- Bundle Build Steps ---------------");

                GUILayout.Space(10.0f);
                CommonEditorUtils.HorizontalLayout(() =>
                {
#if UNITY_2017_1_OR_NEWER
                    m_buildOpt.data = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField("BuildAssetBundleOptions : ", m_buildOpt.data);
#else
                    // EnumMaskField is Incorrect in Unity5
                    m_buildOpt.data = (BuildAssetBundleOptions)EditorGUILayout.MaskField("BuildAssetBundleOptions:", (int)m_buildOpt.data, ms_buildOpts);
#endif
                    GUILayout.Space(20.0f);
                    m_buildUpdateInfo.data = EditorGUILayout.Toggle("Build Update File :", m_buildUpdateInfo.data);
                    m_clearBuildTempFile.data = EditorGUILayout.Toggle("Clean TempBuildFolder :", m_clearBuildTempFile.data);
                });

                GUILayout.Space(10.0f);

                // set build res root
                GUILayout.Space(10.0f);
                CommonEditorUtils.HorizontalLayout(() =>
                {
                    GUILayout.Label(" Build Root : \n " + GameConfig.Instance.assetBuildRoot, new GUIStyle() { fontSize = 20 }, GUILayout.MaxWidth(this.position.width * 0.7f));
                    CommonEditorUtils.VerticalLayout(() =>
                    {
                        if(GUILayout.Button("Change Build Root ...", GUILayout.Height(25.0f)))
                        {
                            var folderExists = System.IO.Directory.Exists(GameConfig.Instance.assetBuildRoot);
                            var newBuildRoot = EditorUtility.OpenFolderPanel("Select Build Root", folderExists ? GameConfig.Instance.assetBuildRoot : Application.dataPath, "");
                            if(string.IsNullOrEmpty(newBuildRoot) == false)
                            {
                                GameConfig.Instance.assetBuildRoot = CommonEditorUtils.FullPathToProjectPath(newBuildRoot);
                            }
                        }

                        if(string.IsNullOrEmpty(GameConfig.Instance.assetBuildRoot) == false)
                        {
                            var buildRootFolder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GameConfig.Instance.assetBuildRoot);
                            var newBuildRootFolder = EditorGUILayout.ObjectField(buildRootFolder, typeof(UnityEngine.Object), false);
                            if(newBuildRootFolder && newBuildRootFolder != buildRootFolder)
                            {
                                var folderPath = AssetDatabase.GetAssetPath(newBuildRootFolder);
                                if(string.IsNullOrEmpty(folderPath) == false)
                                {
                                    if(System.IO.Directory.Exists(folderPath))
                                    {
                                        GameConfig.Instance.assetBuildRoot = CommonEditorUtils.FullPathToProjectPath(folderPath);
                                    }
                                }
                            }
                        }
                    });
                });
                GUILayout.Space(20.0f);


                #region Step 1 : Clear Old Datas
                // clear old datas
                if(GUILayout.Button("Step 1 : Clear Old Datas (Recommand Do This Before Publish...)", GUILayout.Height(35.0f), GUILayout.Width(this.position.width * 0.5f)))
                {
                    if(CommonEditorUtils.MessageBox("Start Clearing ?"))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            if(AutoClearAllAssetBundleNames())
                            {
                                this.ShowNotification(new GUIContent("Clear Successed !!!"));
                            }
                            else
                            {
                                this.ShowNotification(new GUIContent("Clear Cacncelled !!!"));
                            }
                        };
                    }
                    else
                    {
                        this.ShowNotification(new GUIContent("Cancel...!"));
                    }
                }
                #endregion

                GUILayout.Space(10.0f);

                #region Step 2 : Set AssetBundle Names
                CommonEditorUtils.HorizontalLayout(() =>
                {
                    if(GUILayout.Button("Step 2 : Auto Set AssetBundle Names", GUILayout.Height(35.0f), GUILayout.Width(this.position.width * 0.5f)))
                    {
                        if(CommonEditorUtils.MessageBox("Start Auto Set AssetBundle Names ?"))
                        {
                            WrappedProccessAssetBundleName((_baseFolder, _localVersionFolder) =>
                            {
                                return this.AutoSetRunTimeResources(_baseFolder, _localVersionFolder);
                            });
                        }
                        else
                        {
                            this.ShowNotification(new GUIContent("Cancel...!"));
                        }
                    }
                });
                #endregion

                GUILayout.Space(10.0f);

                #region Step 3 : Start Build Bundles
                if(GUILayout.Button("Step 3 : Start Build AssetBundles", GUILayout.Height(35.0f), GUILayout.Width(this.position.width * 0.5f)))
                {
                    if(CommonEditorUtils.MessageBox("Start Build AssetBundle ?"))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            BuildAssetBundles(false);
                        };
                    }
                    else
                    {
                        this.ShowNotification(new GUIContent("Cancel..."));
                    }
                }
                #endregion

                #region Step ALL
                if(GUI.Button(new Rect(this.position.width - 260, this.position.height - 45, 250, 35), "Step ALL(Step1->Step2->Step3)"))
                {
                    StepAll();
                }
                #endregion

                GUILayout.Space(20.0f);

                #region Copy To StreamingAssets
                if(runtimeResourceLoadMode == ResourcesLoadMode.AssetBundle_StreamingAssets
                    || editorResourceLoadMode == ResourcesLoadMode.AssetBundle_StreamingAssets)
                {
                    if(ms_allVersions.Contains(buildVersionStr))
                    {
                        CommonEditorUtils.HorizontalLayout(() =>
                        {
                            GUILayout.Space(this.position.width * 0.1f);
                            if(GUILayout.Button("Copy AssetBundle To StreamingAssets", GUILayout.Height(30.0f), GUILayout.Width(this.position.width * 0.3f)))
                            {
                                CopyTempFolderAssetBundlesToStreamingAssets();
                            }
                        });
                    }
                }
                #endregion
            });
            GUILayout.Space(10.0f);

            #region Shader Collection
            var frameTopLeft = this.position.width * 0.5f + 20;
            GUI.Label(new Rect(frameTopLeft, 10, 300, 30), "Built-In Shader Collection(Sorted) : ");
            GUI.Label(new Rect(frameTopLeft + 350f, 10, 300, 30), "Selected Shaders ");
            var startRect = new Rect(frameTopLeft, 40, 350, 30);
            int collectIndex = 0;
            m_builtinShaderScroll = GUI.BeginScrollView(new Rect(frameTopLeft, 40, 200, 280), m_builtinShaderScroll, new Rect(frameTopLeft, 40, 350, Mathf.Max(300, (ms_collectBuiltinShaders.Count + 1) * 25.0f)));
            {
                foreach(var data in ms_collectBuiltinShaders)
                {
                    var shader = data.shader;
                    var collect = data.collect;
                    if(shader)
                    {
                        var pos = startRect;
                        pos.y = startRect.y + collectIndex * 25.0f;
                        GUI.skin.toggle.fontStyle = (shader == ms_tempTargetedShader) ? FontStyle.BoldAndItalic : FontStyle.Normal;
                        bool value = GUI.Toggle(pos, collect, collectIndex + " : " + shader.name);
                        if(value != collect)
                        {
                            EditorApplication.delayCall += () =>
                            {
                                data.collect = value;
                                if(value == false)
                                {
                                    m_builtinShaderConfigCache.data.data.Remove(shader.name);
                                }
                                else
                                {
                                    m_builtinShaderConfigCache.data.data.GetValue(shader.name).collect = value;
                                }
                                m_builtinShaderConfigCache.Save(true);
                            };
                        }
                        collectIndex++;
                    }
                }
                GUI.skin.toggle.fontStyle = FontStyle.Normal;
            }
            GUI.EndScrollView();

            var frameTopLeft2 = frameTopLeft + 260;
            var collctedStartRect = new Rect(frameTopLeft2, 40, 350, 20);
            collectIndex = 0;
            m_collectedBuiltinShaderScroll = GUI.BeginScrollView(new Rect(frameTopLeft2, 40, 200, 280), m_collectedBuiltinShaderScroll, new Rect(frameTopLeft2, 40, 350, Mathf.Max(300, (m_builtinShaderConfigCache.data.data.Count + 1) * 20.0f)));
            {
                foreach(var data in m_builtinShaderConfigCache.data.data)
                {
                    var pos = collctedStartRect;
                    pos.y = collctedStartRect.y + collectIndex * 20.0f;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if(GUI.Button(pos, collectIndex + " : " + data.Key))
                    {
                        int tagIndex = 0;
                        for(int i = 0; i < ms_collectBuiltinShaders.Count; i++)
                        {
                            var shader = ms_collectBuiltinShaders[i].shader;
                            if(string.Equals(shader.name, data.Key, StringComparison.Ordinal))
                            {
                                tagIndex = i;
                                ms_tempTargetedShader = shader;
                                break;
                            }
                        }
                        m_builtinShaderScroll.y = Mathf.Clamp(25.0f * (tagIndex - 3), 0, float.MaxValue);
                    }
                    collectIndex++;
                }
                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            }
            GUI.EndScrollView();
            #endregion
        }

        // add for avoid GUI stack error
        void OnInspectorUpdate() { Repaint(); }

        void OnDestroy()
        {
            SaveConfig();
            Clear();
        }
        #endregion

        #region GUI
        private void RemoteURL_GUI(System.Func<bool> show)
        {
            if(show())
            {
                var newAddress = EditorGUILayout.TextField("Remote URL : ", GameConfig.Instance.gameConfigSetting.RemoteURL, GUILayout.MaxWidth(this.position.width * 0.45f));
                if(string.Equals(GameConfig.Instance.gameConfigSetting.RemoteURL, newAddress, System.StringComparison.Ordinal) == false)
                {
                    GameConfig.Instance.gameConfigSetting.RemoteURL = newAddress;
                    SaveConfig();
                }
                var newTryTimes = EditorGUILayout.IntField("Download FailedTimes : ", GameConfig.Instance.gameConfigSetting.DownloadTryTimes, GUILayout.MaxWidth(this.position.width * 0.45f));
                newTryTimes = Mathf.Clamp(newTryTimes, 0, 50);
                if(GameConfig.Instance.gameConfigSetting.DownloadTryTimes != newTryTimes)
                {
                    GameConfig.Instance.gameConfigSetting.DownloadTryTimes = newTryTimes;
                    SaveConfig();
                }
            }
        }
        private void WrappedProccessAssetBundleName(System.Func<string, VersionInfo, bool> access, System.Action endCall = null)
        {
            this.ShowNotification(new GUIContent("Setting...!"));
            EditorApplication.delayCall += () =>
            {
                var baseFolder = CommonEditorUtils.LeftSlash(GameConfig.Instance.assetBuildRoot);
                var localVersionFolder = new GameObject(LocalVersion.BundleVersionFileName).AddComponent<VersionInfo>();
                SaveLocalVersion(localVersionFolder);
                bool succ = false;
                try
                {
                    if(succ = access(baseFolder, localVersionFolder))
                    {
                        this.ShowNotification(new GUIContent("AutoSet Successed !!!"));
                    }
                    else
                    {
                        this.ShowNotification(new GUIContent("AutoSet FAILED !!!"));
                    }
                    SaveLocalVersion(localVersionFolder);
                }
                catch(System.Exception ex)
                {
                    Debug.LogError(@ex.ToString());
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                    GameObject.DestroyImmediate(localVersionFolder.gameObject);
                    CommonEditorUtils.SaveAndRefresh(ImportAssetOptions.Default);
                }
                if(succ)
                {
                    if(endCall != null)
                    {
                        endCall.Invoke();
                    }
                }
            };
        }
        #endregion

        #region Help Funcs
        public static void SetGameConfig(string key, object value, bool save = true)
        {
            GameConfig.Instance.SetKeyValue(key, value);
            if(save)
            {
                SaveConfig();
            }
        }
        public static T LoadFromBundleFile<T>(string path, string assetName) where T : UnityEngine.Object
        {
            if(File.Exists(path))
            {
                var ab = AssetBundle.LoadFromFile(path);
                if(ab)
                {
                    T tag = ab.LoadAsset<T>(assetName);
                    ab.Unload(false);
                    return tag;
                }
            }
            return null;
        }
        public static void CopyBuiltAssetFiles(string srcPath, AssetBundleManifest manifest, string destPath, System.Action<float, float> updateCall,
            System.Action endCall)
        {
            if(System.IO.Directory.Exists(destPath))
            {
                FileUtil.DeleteFileOrDirectory(destPath);
            }
            if(System.IO.Directory.Exists(destPath + GameConfig.MetaFileExtName))
            {
                FileUtil.DeleteFileOrDirectory(destPath + GameConfig.MetaFileExtName);
            }
            if(manifest)
            {
                var allAssetBundles = manifest.GetAllAssetBundles();
                CommonEditorUtils.RequireDirectory(destPath);
                float done = 0.0f;
                float total = allAssetBundles.Length + 1;
                foreach(var assetBundle in allAssetBundles)
                {
                    done++;
                    var srcfile = System.IO.Path.Combine(srcPath, assetBundle);
                    var destFile = System.IO.Path.Combine(destPath, assetBundle);
                    var srcFI = new FileInfo(srcfile);
                    CommonEditorUtils.RequireDirectory(System.IO.Path.GetDirectoryName(destFile));
                    srcFI.CopyTo(destFile, true);
                    if(updateCall != null)
                    {
                        updateCall.Invoke(done, total);
                    }
                }
                var manifestBundle = srcPath + "/" + GameConfig.AssetBundleManifestName;
                if(File.Exists(manifestBundle))
                {
                    (new FileInfo(manifestBundle)).CopyTo(destPath + "/" + GameConfig.AssetBundleManifestName, true);
                }
                if(endCall != null)
                {
                    endCall.Invoke();
                }
            }
        }
        public static void TrimBuiltAssetFiles(string srcPath, AssetBundleManifest manifest, string manifestName = null)
        {
            if(manifest)
            {
                srcPath = CommonEditorUtils.LeftSlash(srcPath);
                HashSet<string> bundles = new HashSet<string>(manifest.GetAllAssetBundles());
                if(string.IsNullOrEmpty(manifestName) == false)
                {
                    bundles.Add(manifestName); // add to list avoid delete
                }
                else
                {
                    bundles.Add(GameConfig.AssetBundleManifestName); // add to list avoid delete
                }

                List<string> removeFiles = new List<string>();

                foreach(var file in Directory.GetFiles(srcPath, "*", SearchOption.AllDirectories))
                {
                    if(file.EndsWith(g_manifestExt))
                    {
                        continue;
                    }
                    var filePath = CommonEditorUtils.LeftSlash(file);
                    var bundleFile = filePath.Replace(srcPath, "");
                    if(bundleFile.StartsWith("/"))
                    {
                        bundleFile = bundleFile.Substring(1);
                    }
                    if(bundles.Contains(bundleFile) == false)
                    {
                        removeFiles.Add(filePath);
                    }
                }
                foreach(var data in removeFiles)
                {
                    var manifestFile = data + g_manifestExt;
                    if(File.Exists(manifestFile))
                    {
                        File.Delete(manifestFile);
                    }
                    if(File.Exists(data))
                    {
                        File.Delete(data);
                    }
                }
                var directories = Directory.GetDirectories(srcPath, "*", SearchOption.AllDirectories);
                foreach(var folder in directories)
                {
                    // this may be deleted in this loop
                    if(Directory.Exists(folder))
                    {
                        if(CommonEditorUtils.FastCheckDirectoryIsEmpty(folder))
                        {
                            Directory.Delete(folder, true);
                        }
                    }
                }
            }
        }
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if(!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if(!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach(FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if(copySubDirs)
            {
                foreach(DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
        private void SetToLocalVersion(VersionInfo versionInfo, string filePath, string baseFolder, string assetBundleName)
        {
            try
            {
                string loadPath = CommonEditorUtils.FullPathToResourceLoadPath(filePath, baseFolder);
                if(string.IsNullOrEmpty(loadPath) == false)
                {
                    string assetFileName = Path.GetFileNameWithoutExtension(filePath);
                    string assetFileFullName = Path.GetFileName(filePath);
                    string parentDirectory = Utility.GetDirectoryName(loadPath);
                    var localLoadPath = string.IsNullOrEmpty(parentDirectory) ? assetFileName : (parentDirectory + "/" + assetFileName);
                    var localLoadFullPath = string.IsNullOrEmpty(parentDirectory) ? assetFileFullName : (parentDirectory + "/" + assetFileFullName);
                    versionInfo.AddFileInfo(localLoadPath, assetBundleName);
                    versionInfo.AddFileInfo(localLoadFullPath, assetBundleName);
                }
            }
            catch(System.Exception ex)
            {
                Debug.LogError(@ex.ToString());
            }
        }
        public void SaveLocalVersion(VersionInfo versionInfo)
        {
            versionInfo.BundleVerison = buildVersionStr;
            versionInfo.info = LocalVersion.BundleName;
            var resDir = GameConfig.Instance.assetBuildRoot;
            if(CommonEditorUtils.RequireDirectory(resDir))
            {
#if UNITY_2018_1_OR_NEWER
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(versionInfo.gameObject, CommonEditorUtils.LeftSlash(versionInfoLoadPathToBundleInfo));
#else
                UnityEditor.PrefabUtility.CreatePrefab(CommonEditorUtils.LeftSlash(versionInfoLoadPathToBundleInfo),
                    versionInfo.gameObject, ReplacePrefabOptions.Default);
#endif
                CommonEditorUtils.SetAssetImporterInfo(versionInfoLoadPathToBundleInfo, "versioninfo.ab");
            }
            WrapSetAssetBundleName(versionInfoLoadPathToBundleInfo, GameConfig.Instance.assetBuildRoot, true, null, GameConfig.BundleDefaultExtName, isInFolder: true);
            CommonEditorUtils.SaveAndRefresh();
        }

        private List<LocalVersion.UpdateInfo> GetUpdateInfos(AssetBundleManifest currentManifest)
        {
            var curVersionStr = buildVersionStr;
            int curIndex = ms_allVersions.IndexOf(curVersionStr);
            if(curIndex >= 0)
            {
                List<LocalVersion.UpdateInfo> retVal = new List<LocalVersion.UpdateInfo>();

                for(int i = 0; i < curIndex; i++)
                {
                    var fromVersion = ms_allVersions[i];
                    var fromVersionPath = CommonEditorUtils.LeftSlash(GameConfig.getPlatformFolder + "/" + fromVersion + "/" + GameConfig.AssetBundleManifestName);
                    var fromVersionManifest = LoadFromBundleFile<AssetBundleManifest>(fromVersionPath, GameConfig.AssetBundleManifestName);
                    var updateInfo = ManifestFileCompare(fromVersionManifest, currentManifest);
                    updateInfo.from = new System.Version(fromVersion);
                    updateInfo.to = buildVersion;
                    retVal.Add(updateInfo);
                }

                for(int i = curIndex + 1; i < ms_allVersions.Count; i++)
                {
                    var toVersion = ms_allVersions[i];
                    var toVersionPath = CommonEditorUtils.LeftSlash(GameConfig.getPlatformFolder + "/" + toVersion + "/" + GameConfig.AssetBundleManifestName);
                    var toVersionManifest = LoadFromBundleFile<AssetBundleManifest>(toVersionPath, GameConfig.AssetBundleManifestName);
                    var updateInfo = ManifestFileCompare(currentManifest, toVersionManifest);
                    updateInfo.from = buildVersion;
                    updateInfo.to = new System.Version(toVersion);
                    retVal.Add(updateInfo);
                }
                return retVal;
            }
            return null;
        }
        private static LocalVersion.UpdateInfo ManifestFileCompare(AssetBundleManifest origin, AssetBundleManifest tag)
        {
            var updateFiles = new List<string>();
            var deleteFiles = new List<string>();
            if(origin && tag)
            {
                var originFiles = origin.GetAllAssetBundles();
                var tagFiles = tag.GetAllAssetBundles();
                var originFilesList = new HashSet<string>(originFiles);
                var tagFilesList = new HashSet<string>(tagFiles);

                foreach(var data in tagFilesList)
                {
                    if(originFilesList.Contains(data) == false)
                    {
                        updateFiles.Add(data);
                    }
                    else
                    {
                        var originHash = origin.GetAssetBundleHash(data);
                        var tagHash = tag.GetAssetBundleHash(data);
                        if(tagHash != originHash)
                        {
                            updateFiles.Add(data);
                        }
                    }
                }
                foreach(var data in originFilesList)
                {
                    if(tagFilesList.Contains(data) == false)
                    {
                        deleteFiles.Add(data);
                    }
                }
            }

            var updateInfo = new LocalVersion.UpdateInfo();
            updateInfo.buildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            updateInfo.updateList = updateFiles;
            updateInfo.deleteList = deleteFiles;
            return updateInfo;
        }
        private static void SaveUpdateFile(string savePath, LocalVersion.UpdateInfo info)
        {
            string jsonData = info.ToJson();
            File.WriteAllText(savePath, jsonData);
        }

        public static string SetAssetBundleName(string baseFolder, string assetPath, AssetImporter assetImporter, string assetBundleExt, bool isInFolder, bool strip = true)
        {
            if(assetImporter)
            {
                var relativePath = isInFolder ? assetPath.Substring(baseFolder.Length + 1) : assetPath;
                var assetBundleName = strip ? CommonEditorUtils.StripExtension(relativePath) : relativePath;
                assetImporter.assetBundleName = assetBundleExt != null ? (assetBundleName + assetBundleExt) : assetBundleName;
                return assetImporter.assetBundleName;
            }
            return string.Empty;
        }
        public static AssetImporter SetAssetBundleName(string assetPath, string assetBundleName, string assetBundleExt)
        {
            var assetImporter = AssetImporter.GetAtPath(assetPath);
            if(assetImporter)
            {
                assetImporter.assetBundleName = assetBundleExt != null ? (assetBundleName + assetBundleExt) : assetBundleName;
                return assetImporter;
            }
            return null;
        }

        public AssetImporter WrapSetAssetBundleName(string file, string baseFolder, bool mustSetName,
            Dictionary<string, HashSet<string>> atlasAssets = null,
            string bundleExt = null, bool isInFolder = true)
        {
            var assetPath = file;
            var assetImporter = AssetImporter.GetAtPath(assetPath);
            var assetBundleExt = bundleExt ?? string.Empty;

            if(assetImporter)
            {
                if(AtlasAssetSetting(assetImporter, assetBundleExt, atlasAssets) == false)
                {
                    string assetBundleName = string.Empty;
                    if(AssetPostCheck(assetPath, ref assetBundleName) && string.IsNullOrEmpty(assetBundleName) == false)
                    {
                        assetImporter.assetBundleName = assetBundleName + assetBundleExt;
                    }
                    else
                    {
                        if(mustSetName)
                        {
                            SetAssetBundleName(baseFolder, assetPath, assetImporter, assetBundleExt, isInFolder);
                        }
                    }
                }
            }
            return assetImporter;
        }

        // check is resource of unity readable
        public static bool CheckIsResources(string fileName)
        {
            var ext = System.IO.Path.GetExtension(fileName);
            if(string.IsNullOrEmpty(ext) == false)
            {
                if(string.Equals(ext, ".meta", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if(string.Equals(ext, ".cs", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if(string.Equals(ext, ".js", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if(string.Equals(ext, ".boo", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if(string.Equals(ext, ".dll", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }
        // pos check the asset, some assets are not set correctly for runtime, must change it
        private bool AssetPostCheck(string loadPath, ref string assetBundleName)
        {
            return ProcessMaterial(loadPath, ref assetBundleName);
        }

        // set sprite to atlas
        private static bool AtlasAssetSetting(AssetImporter importer, string bundleExt, Dictionary<string, HashSet<string>> atlasAssets)
        {
            var textureImporter = importer as TextureImporter;
            if(textureImporter)
            {
                bool isAtlas = (textureImporter.textureType == TextureImporterType.Sprite && false == string.IsNullOrEmpty(textureImporter.spritePackingTag));
                if(isAtlas)
                {
                    // set asset bundle name
                    var assetBundleExt = bundleExt ?? string.Empty;
                    textureImporter.assetBundleName = (g_atlasHeader + textureImporter.spritePackingTag + assetBundleExt).ToLower();
                    // make atlas mark
                    var atlasLoadPath = GameConfig.SpriteAtlasFolderEditor + "/" + textureImporter.spritePackingTag + g_spriteAtlasExt;
                    var spriteList = atlasAssets.GetValue(atlasLoadPath);
                    var guid = AssetDatabase.AssetPathToGUID(importer.assetPath);
                    var assetInfo = spriteFormat.Replace("#KEY#", guid);
                    spriteList.Add(assetInfo);
                }
                return isAtlas;
            }
            return false;
        }
        // set texture2D readable
        public static void Texture2DReadable(Texture2D tex2D)
        {
            if(tex2D)
            {
                var path = AssetDatabase.GetAssetPath(tex2D);     // texture path
                var importer = TextureImporter.GetAtPath(path) as TextureImporter;
                if(importer && false == importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
            }
        }

        private void VersionChanged(string newVersionStr)
        {
            if(CheckVersionString(newVersionStr))
            {
                Debug.Log("Version is " + newVersionStr);
                buildVersion = new System.Version(newVersionStr);
                CheckAllVersions();
            }
        }
        private void CheckAllVersions()
        {
            try
            {
                ms_allVersions.Clear();
                if(System.IO.Directory.Exists(GameConfig.getPlatformFolder))
                {
                    foreach(var versionFile in Directory.GetDirectories(GameConfig.getPlatformFolder, "*", SearchOption.TopDirectoryOnly))
                    {
                        var path = CommonEditorUtils.LeftSlash(versionFile);
                        var manifestFilePath = path + "/" + GameConfig.AssetBundleManifestName;
                        var manifest = LoadFromBundleFile<AssetBundleManifest>(manifestFilePath, GameConfig.AssetBundleManifestName);
                        if(manifest)
                        {
                            var folderName = path.Substring(path.LastIndexOf('/') + 1);
                            if(CheckVersionString(folderName))
                            {
                                if(ms_allVersions.Contains(folderName) == false)
                                {
                                    ms_allVersions.Add(folderName);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // must catch just in case
            }
            finally
            {
                AllVerisonSort();
            }
        }
        private void AllVerisonSort()
        {
            if(ms_allVersions.Count > 0)
            {
                ms_allVersions.Sort((_l, _r) =>
                {
                    var lVer = new System.Version(_l);
                    var rVer = new System.Version(_r);
                    if(lVer > rVer)
                    {
                        return 1;
                    }
                    if(lVer < rVer)
                    {
                        return -1;
                    }
                    return 0;
                });
            }
        }
        private void VersionsGUI()
        {
            if(ms_allVersions != null && ms_allVersions.Count > 0)
            {
                foreach(var compareVersion in ms_allVersions)
                {
                    if(GUILayout.Button("Version : " + compareVersion, GUILayout.Width(150.0f), GUILayout.Height(20.0f)))
                    {
                        EditorApplication.delayCall += () =>
                        {
                            VersionChanged(compareVersion);
                            string folderPath = GameConfig.getPlatformFolder + "/" + compareVersion;
                            if(System.IO.Directory.Exists(folderPath))
                            {
                                System.Diagnostics.Process.Start(folderPath);
                            }
                        };
                    }
                }
            }
        }
        public static bool CheckVersionString(string versionStr)
        {
            try
            {
                var version = new System.Version(versionStr);
                var str = version.ToString(3);
                return string.IsNullOrEmpty(str) == false;
            }
            catch
            {
                return false;
            }
        }

        // asset bundle name combine
        public static string GetAssetBundleFullName(AssetImporter assetImporter)
        {
            if(assetImporter)
            {
                var retName = assetImporter.assetBundleName;
                if(string.IsNullOrEmpty(assetImporter.assetBundleVariant) == false)
                {
                    retName = string.Concat(retName, ".", assetImporter.assetBundleVariant);
                }
                return retName;
            }
            return string.Empty;
        }

        private bool ProcessMaterial(string path, ref string assetBundleName)
        {
            if(m_builtinShaderConfigCache.data.data.Count > 0)
            {
                if(path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(material != null)
                    {
                        if(material.shader)
                        {
                            ShaderConfigInfo config;
                            if(m_builtinShaderConfigCache.data.data.TryGetValue(material.shader.name, out config) && config != null && config.collect)
                            {
                                assetBundleName = g_shaderCollectionFolder + material.shader.name;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        #endregion

        #region Special Funcs
        /// <summary>
        /// Editor settings fix, if you sure what you need, change it your self
        /// </summary>
        public static void EditorConfigSettings()
        {
#if UNITY_2017_1_OR_NEWER
            UnityEditor.EditorSettings.spritePackerMode = SpritePackerMode.AlwaysOnAtlas;   // pack atlas
#else
            UnityEditor.EditorSettings.spritePackerMode = SpritePackerMode.AlwaysOn;       // pack atlas
#endif

            // set Graphics for avoid lightmap stripped
            SerializedObject graphicsManager = null;
#if UNITY_2017_1_OR_NEWER
            using(graphicsManager = new SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsAssetPath)[0]))
#else
            graphicsManager = new SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsAssetPath)[0]);
#endif
            {
                SerializedProperty m_LightmapStripping = graphicsManager.FindProperty("m_LightmapStripping");
                m_LightmapStripping.intValue = 1;
                SerializedProperty m_LightmapKeepPlain = graphicsManager.FindProperty("m_LightmapKeepPlain");
                m_LightmapKeepPlain.boolValue = true;
                SerializedProperty m_LightmapKeepDirCombined = graphicsManager.FindProperty("m_LightmapKeepDirCombined");
                m_LightmapKeepDirCombined.boolValue = true;
                SerializedProperty m_LightmapKeepDynamicPlain = graphicsManager.FindProperty("m_LightmapKeepDynamicPlain");
                m_LightmapKeepDynamicPlain.boolValue = true;
                SerializedProperty m_LightmapKeepDynamicDirCombined = graphicsManager.FindProperty("m_LightmapKeepDynamicDirCombined");
                m_LightmapKeepDynamicDirCombined.boolValue = true;
                SerializedProperty m_LightmapKeepShadowMask = graphicsManager.FindProperty("m_LightmapKeepShadowMask");
                m_LightmapKeepShadowMask.boolValue = true;
                SerializedProperty m_LightmapKeepSubtractive = graphicsManager.FindProperty("m_LightmapKeepSubtractive");
                m_LightmapKeepSubtractive.boolValue = true;
                graphicsManager.ApplyModifiedProperties();
            }
        }
        // Get all Built-In Shaders, these shaders may cause Performance issues
        public static HashSet<Shader> GetBuiltInShaders()
        {
            HashSet<Shader> list = new HashSet<Shader>();

#if UNITY_2018_1_OR_NEWER
            // get built-in shaders is different from 2018
            var shaderInfos = ShaderUtil.GetAllShaderInfo();
            foreach(var shaderInfo in shaderInfos)
            {
                var shader = Shader.Find(shaderInfo.name);
                if(shader)
                {
                    var path = AssetDatabase.GetAssetPath(shader);
                    var shaderAsset = AssetDatabase.LoadAssetAtPath<Shader>(path);
                    if(shaderAsset == false)
                    {
                        list.Add(shader);
                    }
                }
            }
#else
            // get built-in shaders works in Unity5/Unity2017/Unity2019...
            var standardShader = Shader.Find("Standard");
            if(standardShader)
            {
                var path = AssetDatabase.GetAssetPath(standardShader);
                var builtinAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                if(builtinAssets != null)
                {
                    foreach(var builtinAsset in builtinAssets)
                    {
                        var shaderAsset = builtinAsset as Shader;
                        if(shaderAsset)
                        {
                            list.Add(shaderAsset);
                        }
                    }
                }
            }
#endif
            return list;
        }
        /// <summary>
        /// you can only create SpriteAtlas by yaml, 
        /// Notice : [bindAsDefault: 0/1] shows in inspector of SpriteAtlas -> Include in Build, big diferent with 2 modes
        /// VersionInfo : a trick applied for late bind
        /// </summary>
        /// <param name="versionInfo"></param>
        /// <param name="atlasAssets"></param>
        public static void CreateSpriteAtlasAssets(VersionInfo versionInfo, Dictionary<string, HashSet<string>> atlasAssets)
        {
            const string Key = "#SpriteFormat#";
            const string yaml = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!687078895 &4343727234628468602
SpriteAtlas:
  m_ObjectHideFlags: 0
  m_PrefabParentObject: {fileID: 0}
  m_PrefabInternal: {fileID: 0}
  m_Name: New Sprite Atlas
  m_EditorData:
    textureSettings:
      serializedVersion: 2
      anisoLevel: 1
      compressionQuality: 50
      maxTextureSize: 8192
      textureCompression: 2
      filterMode: 1
      generateMipMaps: 0
      readable: 0
      crunchedCompression: 1
      sRGB: 1
    platformSettings: []
    packingParameters:
      serializedVersion: 2
      padding: 4
      blockOffset: 1
      allowAlphaSplitting: 0
      enableRotation: 0
      enableTightPacking: 0
    variantMultiplier: 1
    packables:
" + Key +
@"
    bindAsDefault: 1
  m_MasterAtlas: {fileID: 0}
  m_PackedSprites: []
  m_PackedSpriteNamesToIndex: []
  m_Tag: New Sprite Atlas
  m_IsVariant: 0
";
            try
            {
                StringBuilder sb = new StringBuilder();
                var atlasFolder = GameConfig.SpriteAtlasFolderEditor;   // Atlas folder only have spriteatlas files, clear all other files
                HashSet<string> oldFiles = new HashSet<string>();
                if(System.IO.Directory.Exists(atlasFolder))
                {
                    var atlasFiles = System.IO.Directory.GetFiles(atlasFolder, "*.*", SearchOption.AllDirectories);
                    if(atlasFiles != null)
                    {
                        foreach(var atlasFile in atlasFiles)
                        {
                            oldFiles.Add(CommonEditorUtils.LeftSlash(atlasFile));
                        }
                    }
                }
                foreach(var data in atlasAssets)
                {
                    sb.Length = 0;
                    var fullPath = data.Key;
                    var assetInfos = data.Value;
                    var driectory = System.IO.Path.GetDirectoryName(fullPath);
                    CommonEditorUtils.RequireDirectory(driectory);
                    int index = 0;
                    foreach(var asset in assetInfos)
                    {
                        if(index > 0)
                        {
                            sb.Append(System.Environment.NewLine);
                        }
                        sb.Append(asset);
                        index++;
                    }
                    string writeString = yaml.Replace(Key, sb.ToString());
                    oldFiles.Remove(fullPath);
                    System.IO.File.WriteAllText(fullPath, writeString, System.Text.Encoding.UTF8);
                }

                foreach(var oldFile in oldFiles)
                {
                    AssetDatabase.DeleteAsset(oldFile);
                }

                CommonEditorUtils.SaveAndRefresh(ImportAssetOptions.ForceSynchronousImport);    // save if has new atlas

                foreach(var data in atlasAssets)
                {
                    var fullPath = data.Key;
                    var importer = AssetImporter.GetAtPath(fullPath);
                    if(importer)
                    {
                        var assetBundleName = (g_atlasHeader + System.IO.Path.GetFileNameWithoutExtension(data.Key) + GameConfig.BundleDefaultExtName).ToLower();
                        importer.assetBundleName = assetBundleName;
                        // make the atlas can be found in VerisonInfo, so that we can adapt UnityEngine.U2D.SpriteAtlasManager.atlasRequested call in case
                        string loadPath = fullPath.Replace(g_spriteAtlasExt, "");
                        versionInfo.AddFileInfo(loadPath, assetBundleName);
                    }
                }

                CommonEditorUtils.SaveAndRefresh(ImportAssetOptions.Default);    // save asset bundle names
            }
            catch(System.Exception ex)
            {
                Debug.LogError(@ex.ToString());
            }
        }
        #endregion

        #region All Asset Proccess
        public class ReferenceAssetProccess
        {
            public string assetBasePath { get; protected set; }
            public Containers.OrderedDictionary<string, HashSet<string>> refCounter = new Containers.OrderedDictionary<string, HashSet<string>>();  // inversed reference
            private System.Action<string, string> m_normalAssetBundleSet = null;

            public delegate bool MaterialProcess(string path, ref string assetBundleName);
            public MaterialProcess materialProccess = null;

            private HashSet<string> mainAssets = new HashSet<string>();
            private HashSet<string> checkedAssets = new HashSet<string>();

            public System.Func<string, string, bool> conflict = null;

            public int confictedCount { get; private set; }

            public ReferenceAssetProccess(string assetBaseFolder, System.Action<string, string> normalAssetBundleSet, HashSet<string> mainAssets)
            {
                this.assetBasePath = assetBaseFolder;
                this.m_normalAssetBundleSet = normalAssetBundleSet;
                this.mainAssets = mainAssets;
            }

            #region Main Funcs
            public void Proccess(string assetLoadPath)
            {
                if(checkedAssets.Contains(assetLoadPath) == false)
                {
                    checkedAssets.Add(assetLoadPath);
                    var dps = AssetDatabase.GetDependencies(assetLoadPath, false);
                    if(dps != null && dps.Length > 0)
                    {
                        for(int i = 0; i < dps.Length; i++)
                        {
                            var dp = dps[i];
                            if(CheckIsValidAsset(dp))
                            {
                                if(CheckIsMarkableAsset(dp))
                                {
                                    refCounter.GetValue(dp).Add(assetLoadPath);
                                }
                                Proccess(dp);
                            }
                        }
                    }
                }
            }

            public void SetAssetBundleNames(System.Func<float, float, float, bool> progress = null)
            {
                foreach(var data in mainAssets)
                {
                    refCounter.Remove(data);        // Trim out the main assets
                }
                if(materialProccess != null)
                {
                    refCounter.RemoveAll((_key, _value) =>
                    {
                        string materialAssetBundleName = null;
                        var succ = materialProccess.Invoke(_key, ref materialAssetBundleName);
                        if(succ)
                        {
                            SetAssetBundleName(_key, materialAssetBundleName, GameConfig.BundleDefaultExtName);
                        }
                        return succ;
                    });
                }

                var searchedCache = new HashSet<string>();
                confictedCount = 0;
                float count = 0.0f;
                float maxCount = refCounter.Count;
                float maxCountInv = maxCount > 0 ? (1f / maxCount) : 1f;
                foreach(var refTargets in refCounter)
                {
                    count++;
                    if(progress != null)
                    {
                        if(progress.Invoke(count, maxCount, count * maxCountInv) == false)
                        {
                            return;
                        }
                    }

                    var assetLoadPath = refTargets.Key;
                    var refList = refTargets.Value;
                    if(refList != null)
                    {
                        if(refList.Count == 1)
                        {
                            var assetImporter = AssetImporter.GetAtPath(assetLoadPath);
                            if(assetImporter)
                            {
                                searchedCache.Clear();
                                var tagAssetLoadPath = GetFinalRefAssetLoadPath(assetLoadPath, searchedCache);
                                if(searchedCache.Count > 1)
                                {
                                    bool conflicted = false;
                                    // check they has same name and conflicted? dont put them into same assetbundle. for exp: prefab-model, it may load the model asset
                                    if(conflict != null)
                                    {
                                        confictedCount++;
                                        var tagAssetLoadName = System.IO.Path.GetFileNameWithoutExtension(tagAssetLoadPath);
                                        var curAssetLoadName = System.IO.Path.GetFileNameWithoutExtension(assetLoadPath);
                                        if(string.Equals(tagAssetLoadName, curAssetLoadName, System.StringComparison.Ordinal))
                                        {
                                            if(conflict.Invoke(tagAssetLoadPath, assetLoadPath))
                                            {
                                                SetAssetBundleName(assetBasePath, assetLoadPath, assetImporter, GameConfig.BundleDefaultExtName, false);
                                                conflicted = true;
                                            }
                                        }
                                    }
                                    if(false == conflicted)
                                    {
                                        assetImporter.assetBundleName = AssetImporter.GetAtPath(tagAssetLoadPath).assetBundleName;
                                    }
                                }
                                else
                                {
                                    SetAssetBundleName(assetBasePath, tagAssetLoadPath, assetImporter, GameConfig.BundleDefaultExtName, false);
                                }
                            }
                        }
                        else
                        {
                            if(m_normalAssetBundleSet != null)
                            {
                                m_normalAssetBundleSet.Invoke(assetLoadPath, assetBasePath);
                            }
                        }
                    }
                }
            }
            #endregion

            #region Help Funcs
            // scene is not collection of asset
            protected static bool CheckIsMarkableAsset(string assetLoadPath)
            {
                return assetLoadPath.EndsWith(g_sceneExt) == false;
            }

            protected static bool CheckIsValidAsset(string fileName)
            {
                if(fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
#if !UNITY_2017_1_OR_NEWER
                if(fileName.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if(fileName.EndsWith(".boo", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
#endif
                return true;
            }

            protected static bool CheckIsMaterial(string fileName)
            {
                return fileName.EndsWith(".mat", StringComparison.OrdinalIgnoreCase);
            }

            protected string GetFinalRefAssetLoadPath(string assetLoadPath, HashSet<string> searchedCache)
            {
                if(searchedCache.Contains(assetLoadPath))
                {
                    return assetLoadPath;
                }
                searchedCache.Add(assetLoadPath);
                var refList = refCounter.TryGetValue(assetLoadPath);
                if(refList != null && refList.Count == 1)
                {
                    var tagLoadPath = refList.FirstOrDefault();
                    if(CheckIsMarkableAsset(tagLoadPath))
                    {
                        assetLoadPath = GetFinalRefAssetLoadPath(tagLoadPath, searchedCache);
                    }
                }
                return assetLoadPath;
            }
            #endregion
        }
        #endregion

    }
}