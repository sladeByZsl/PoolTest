using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.Common;
    using AssetBundleMaster.GameUtilities;
    using AssetBundleMaster.Extention;
    using TypeDataPool = ObjectPool.GlobalAllocator<AssetLoadManager.TypeData>;            // obj pool
    using TypeDataListPool = ObjectPool.GlobalAllocator<List<AssetLoadManager.TypeData>>;  // obj pool

    public enum ResourcesLoadMode
    {
        Resources = 1,                          // load orgin resource -- not bundled
        AssetBundle_StreamingAssets = 2,        // build assetbundle to streaming assets
        AssetBundle_PersistentDataPath = 3,     // release assetbundle to PersistentDataPath
        AssetBundle_Remote = 4,                 // load from resource servre by webrequest
#if UNITY_EDITOR
        AssetBundle_EditorTest = 5,             // test mode in editor
        AssetDataBase_Editor = 6,               // straight load in editor
#endif
    }

    // load mark for Async / Sync
    public enum LoadThreadMode
    {
        Asynchronous = 1,
        Synchronous = 2,
    }

    public class AssetLoadManager : SingletonComponent<AssetLoadManager>
    {
        #region Defines
        public class TypeData
        {
            public Type type { get; private set; }
            public AssetLoaderBase loader { get; private set; }

            public TypeData Set(Type type, AssetLoaderBase value)
            {
                this.type = type;
                this.loader = value;
                return this;
            }

            public void Clear()
            {
                type = null;
                loader = null;
            }

            public static implicit operator AssetLoader(TypeData typeData)
            {
                return typeData != null ? typeData.loader as AssetLoader : null;
            }
            public static implicit operator AssetList(TypeData typeData)
            {
                return typeData != null ? typeData.loader as AssetList : null;
            }
            public static implicit operator LevelLoader(TypeData typeData)
            {
                return typeData != null ? typeData.loader as LevelLoader : null;
            }
        }

        public class CountDown
        {
            public float countDownTime = 0.0f;
            public bool started { get; private set; }
            public float startTime { get; private set; }
            private float timeNow
            {
                get
                {
#if UNITY_EDITOR
                    return Time.time;
#else
                    return Time.unscaledTime;
#endif
                }
            }
            public float waitingTime
            {
                get
                {
                    return started ? startTime + countDownTime - timeNow : 0.0f;
                }
            }

            public void Start()
            {
                started = true;
                startTime = timeNow;
            }

            public bool Finished()
            {
                bool finished = started && (timeNow - startTime) >= countDownTime;
                if(finished)
                {
                    started = false;
                }
                return finished;
            }

            public void Stop()
            {
                started = false;
            }
        }

        public struct AssetLoadInfo
        {
            public bool isBundleMode;
            public string assetLoadPath;
            public System.Type systemTypeInstance;
            public LoadThreadMode loadThreadMode;
            public bool unloadable;
            public AssetList assetList;
        }

        public class Configs
        {
            public bool isBundleMode;
            public bool isResourcesMode;
            public bool isRemoteAssets;
            public int downloadTimes;
            public bool forceUnloadMode;

            public static Configs LoadFromGameConfig(GameConfig gameConfig)
            {
                var configs = new Configs();
                configs.isBundleMode = gameConfig.isBundleMode;
                configs.isResourcesMode = gameConfig.isResourcesMode;
                configs.isRemoteAssets = gameConfig.isRemoteAssets;
                configs.downloadTimes = gameConfig.gameConfigSetting.DownloadTryTimes;
                configs.forceUnloadMode = gameConfig.isResourcesMode;
#if UNITY_EDITOR
                configs.forceUnloadMode |= gameConfig.isEditorMode;
#endif
                return configs;
            }
        }
        #endregion

        #region Variables
        // wait time for unloading assets
        [System.NonSerialized]
        public float unloadPaddingTime = 30f;
        [System.NonSerialized]
        public int minUnloadAssetCounter = 60;        // the auto unload will be call after _unusedAssetCount is greater than minUnloadAssetCounter
        #endregion

        #region cached datas
        // assetbundle loader cache
        private Dictionary<string, AssetBundleLoader> _assetBundleRequests = new Dictionary<string, AssetBundleLoader>();

        // asset loaders
        private Dictionary<string, List<TypeData>> _assetLoaders = new Dictionary<string, List<TypeData>>();
        // asset list loaders
        private Dictionary<string, List<TypeData>> _assetLists = new Dictionary<string, List<TypeData>>();
        // level loaders
        private Dictionary<string, List<TypeData>> _levelLoaders = new Dictionary<string, List<TypeData>>();

        // a hash reference for the asset loader
        private Dictionary<int, AssetList> _assetLoaderHashDict = new Dictionary<int, AssetList>();
        private Dictionary<int, LevelLoader> _levelLoaderHashDict = new Dictionary<int, LevelLoader>();

        // marks different level load with same asset bundle name
        private Dictionary<string, string> _uniqueLevelNames = new Dictionary<string, string>();

        // control marks
        private bool _unloadUnloadedLoaders = false;
        private static int _unusedAssetCount = 0;

        private System.Text.StringBuilder _uniqueNameSB = new System.Text.StringBuilder();

        private static readonly CountDown ms_assetUnloadCountDown = new CountDown();
        private static readonly CountDown ms_resourceUnloadCountDown = new CountDown();
        private CountDown m_inverseUnloadCountDown = null;

        private Common.QueueCommands m_initWait = null;

        private static readonly HashSet<string> ms_unloadedAssets = new HashSet<string>();
        private static readonly HashSet<string> ms_unloadingAssets = new HashSet<string>();
        #endregion

        #region Properties
        public bool Prepared { get; private set; }

        public AssetBundleManifest assetBundleManifest { get; private set; }
        public Configs configs { get; private set; }

#if UNITY_EDITOR
        public float unloadAssetsWaitingTime { get { return ms_assetUnloadCountDown.waitingTime; } }
        public float unloadResourcesWaitingTime { get { return ms_resourceUnloadCountDown.waitingTime; } }
#endif
        #endregion

        #region Events
        public static event System.Action<HashSet<string>> onResourcesUnloaded = null;
        #endregion

        #region Override Funcs
        protected override void Initialize()
        {
            CoroutineRoot.Create();
            Prepared = false;
            configs = Configs.LoadFromGameConfig(GameConfig.Instance);

            // pools
            TypeDataPool.Set(create: () => { return new TypeData(); }, deAllocate: (_typeData) =>
            {
                ms_unloadingAssets.Add(_typeData.loader.loadPath);
                _typeData.Clear();
            });
            TypeDataListPool.Set(create: () => { return new List<TypeData>(); }, deAllocate: (_list) =>
            {
                _list.Clear();
            });

            Debug.Log("Program Basic Version is : " + GameConfig.Instance.gameConfigSetting.Version);
            Debug.Log("Asset Load Mode is : " + GameConfig.Instance.resourceLoadMode);
#if UNITY_EDITOR
            Debug.Log("Asset Load Root is : " + GameConfig.Instance.ShowLoadRootPath());
#endif

            if(configs.isBundleMode)
            {
                LoadAssetBundleManifest();
                m_inverseUnloadCountDown = new CountDown() { countDownTime = 2.0f };
                m_inverseUnloadCountDown.Start();
            }
            AssetList.assetChanged += OnAssetListAssetChanged;
            CheckPrepared();
        }
        protected override void UnInitialize()
        {
            throw new Exception("Don't Destroy AssetLoadManager in Runtime !");
        }
        #endregion

        #region Main Funcs
        /// <summary>
        /// only if the asset bundle load from remote url, should do load assets after Inided
        /// </summary>
        /// <param name="onloaded"></param>
        public void OnAssetLoadModuleInited(System.Action onloaded)
        {
            if(m_initWait != null)
            {
                m_initWait.Enqueue(new Command() { cmd = (_) => { onloaded.Invoke(); } });
            }
            else
            {
                onloaded.Invoke();
            }
        }

        /// <summary>
        /// Load Assets From Path, their are multi assets in a same load path maybe
        /// </summary>
        /// <param name="assetLoadPath"></param>
        /// <param name="loadAllAssets"></param>
        /// <param name="systemTypeInstance"></param>
        /// <returns></returns>
        public UnityEngine.Object[] LoadAssets(string assetLoadPath, System.Type systemTypeInstance, bool unloadable)
        {
            var list = LoadAssetList(assetLoadPath, systemTypeInstance, unloadable);
            return list.Assets;
        }
        // intermedia func
        public AssetList LoadAssetList(string assetLoadPath, System.Type systemTypeInstance, bool unloadable)
        {
            var list = LoadAssetsWrapped(assetLoadPath, false, systemTypeInstance, LoadThreadMode.Synchronous, unloadable, null);
            return list;
        }


        /// <summary>
        /// Load All Assets From Path Async
        /// </summary>
        /// <param name="assetLoadPath"></param>
        /// <param name="loadAllAssets"></param>
        /// <param name="systemTypeInstance"></param>
        /// <param name="loaded"></param>
        public AssetList LoadAssetsAsync(string assetLoadPath, System.Type systemTypeInstance, bool unloadable,
            System.Action<UnityEngine.Object[]> loaded)
        {
            var list = LoadAssetsWrapped(assetLoadPath, false, systemTypeInstance, LoadThreadMode.Asynchronous, unloadable, loaded);
            return list;
        }

        /// <summary>
        /// Load All Assets From Path, Folder load integrated
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetLoadPath"></param>
        /// <returns></returns>
        public UnityEngine.Object[] LoadAllAssets(string assetLoadPath, System.Type systemTypeInstance, bool unloadable)
        {
            var list = LoadAllAssetList(assetLoadPath, systemTypeInstance, unloadable);
            return list.Assets;
        }
        // intermedia func
        public AssetList LoadAllAssetList(string assetLoadPath, System.Type systemTypeInstance, bool unloadable)
        {
            var list = LoadAssetsWrapped(assetLoadPath, true, systemTypeInstance, LoadThreadMode.Synchronous, unloadable, null);
            return list;
        }

        /// <summary>
        ///  Load All Assets From Path Async, Folder load integrated
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetLoadPath"></param>
        /// <param name="loaded"></param>
        public AssetList LoadAllAssetsAsync(string assetLoadPath, System.Type systemTypeInstance, bool unloadable, System.Action<UnityEngine.Object[]> loaded)
        {
            return LoadAssetsWrapped(assetLoadPath, true, systemTypeInstance, LoadThreadMode.Asynchronous, unloadable, loaded);
        }

        /// <summary>
        /// base load all func
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetLoadPath"></param>
        /// <param name="loadThreadMode"></param>
        /// <param name="loadAll"></param>
        /// <param name="loaded"></param>
        /// <returns></returns>
        private AssetList LoadAssetsWrapped(string assetLoadPath,
            bool loadAll,
            System.Type systemTypeInstance,
            LoadThreadMode loadThreadMode,
            bool unloadable,
            System.Action<UnityEngine.Object[]> loaded)
        {
            var typeData = GetTypeData(_assetLists, assetLoadPath, systemTypeInstance);
            AssetList assetList = typeData;     // auto cast
            bool isNewAssetList = (assetList == null || assetList.unloaded);
            bool newLoad = isNewAssetList || (loadAll && assetList.loadAll != loadAll);
            if(newLoad)
            {
                if(isNewAssetList)
                {
                    assetList = new AssetList(assetLoadPath, systemTypeInstance, loadThreadMode, loadAll);
                    typeData.Set(systemTypeInstance, assetList);
                }

                AssetLoadInfo assetLoadInfo = new AssetLoadInfo()
                {
                    isBundleMode = configs.isBundleMode,
                    assetLoadPath = assetLoadPath,
                    systemTypeInstance = systemTypeInstance,
                    loadThreadMode = loadThreadMode,
                    unloadable = unloadable,
                    assetList = assetList
                };
                AccessLoadPathFiles(assetLoadPath, assetLoadInfo, (_filePath, _assetName, _assetLoadInfo) =>
                {
                    // the load mode diferent, the loadpath and assetbundle name is inverted
                    var assetLoader = AssetLoadManager.Instance.GetUniqueAssetLoader(_assetLoadInfo.isBundleMode ? _assetLoadInfo.assetLoadPath : _filePath,
                        _assetLoadInfo.isBundleMode ? _filePath : _assetLoadInfo.assetLoadPath,
                        _assetName,
                        _assetLoadInfo.systemTypeInstance,
                        _assetLoadInfo.loadThreadMode,
                        _assetLoadInfo.unloadable);
                    _assetLoadInfo.assetList.AddLoader(assetLoader);
                });
            }
            assetList.unloadable &= unloadable;
            AssetListLoadRequest(assetList, loadThreadMode, loadAll, loaded);
            return assetList;
        }

        /// <summary>
        /// Unload asset by has code
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="unloadImmediate"></param>
        public void UnloadAsset(int hashCode)
        {
            var assetList = _assetLoaderHashDict.TryGetValue(hashCode);
            if(assetList != null)
            {
                _assetLoaderHashDict.Remove(hashCode);
                if(assetList.Unload())
                {
                    if(configs.isBundleMode == false)
                    {
                        _unusedAssetCount++;    // need to call Resources.UnloadUnusedAssets();
                    }
                }
                ClearRequest();
            }
        }

        /// <summary>
        /// Load Level
        /// Notice: the LevelLoaded callback, Unity load scene in an Async way even in Sync Mode!
        /// Notice: the same named level is the same in memory though they are not the same assetbundle name!
        /// </summary>
        /// <param name="levelLoadPath"></param>
        /// <param name="loadMode"></param>
        /// <param name="loadSceneMode"></param>
        /// <param name="callBack"> even Async Mode, should wait for the call back </param>
        /// <returns></returns>
        public int LoadLevel(string levelLoadPath, LoadThreadMode loadMode, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode,
            System.Action<int, UnityEngine.SceneManagement.Scene> callBack)
        {
            int hashCode = 0;
            var levelLoader = GetLevelLoader(levelLoadPath, loadSceneMode, loadMode, true);   // level loader alywas new         
            hashCode = levelLoader.GetHashCode();
            if(callBack != null)
            {
                // complete call is from SceneManagement
                levelLoader.completed.PushCall((_loader) =>
                {
                    var _levelLoader = _loader as LevelLoader;
                    if(_levelLoader != null)
                    {
                        if(callBack != null)
                        {
                            callBack.Invoke(_levelLoader.GetHashCode(), _levelLoader.scene);    // call back should not ref an outer data
                        }
                    }
                });
            }
            if(levelLoader.assetBundleLoader != null)
            {
                levelLoader.assetBundleLoader.IncreaseReferenceCount(1);
            }
            levelLoader.LoadRequest(loadMode);
            return hashCode;
        }

        /// <summary>
        /// Unload loaded / loading scene with unique ID
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="unloaded"></param>
        /// <param name="unloadImmediate"></param>
        /// <returns></returns>
        public bool UnloadLevel(int hashCode, System.Action<int, UnityEngine.SceneManagement.Scene> unloaded = null)
        {
            var levelLoader = _levelLoaderHashDict.TryGetValue(hashCode);
            if(levelLoader != null)
            {
                _levelLoaderHashDict.Remove(hashCode);
                // unload scene and assets
                levelLoader.UnloadScene((_scene) =>
                {
                    ClearRequest();
                    if(false == levelLoader.unloadable || configs.forceUnloadMode)
                    {
                        TickUnloadedLoaders();
                        if(_levelLoaders.ContainsKey(levelLoader.loadPath) == false)
                        {
                            _unusedAssetCount++;
                            if(ms_assetUnloadCountDown.started == false)
                            {
                                RequestUnloadUnusedAssets();    // force to unload in case the Scene assets is already unloaded
                            }
                        }
                    }

                    if(unloaded != null)
                    {
                        unloaded.Invoke(hashCode, _scene);
                    }
                });
                return true;
            }
            return false;
        }

        /// <summary>
        /// Unload scene assets only, keep the scene alive
        /// </summary>
        /// <param name="hashCode"></param>
        /// <returns></returns>
        public bool UnloadLevelAssets(int hashCode)
        {
            var levelLoader = _levelLoaderHashDict.TryGetValue(hashCode);
            if(levelLoader != null)
            {
                levelLoader.ChangeAllUnloadable(false);     // scene assets changed to be not unloadable
                levelLoader.Unload(false);                  // unload scene assets, bit keep ref in _assetLoaderHash, don't remove it
                ClearRequest();
                return true;
            }
            return false;
        }
        #endregion

        #region Help Funcs
        /// <summary>
        /// Access load path, get real asset load path
        /// </summary>
        /// <param name="loadPath"></param>
        /// <param name="filePathAccess"></param>
        public void AccessLoadPathFiles(string loadPath, AssetLoadInfo assetLoadInfo, System.Action<string, string, AssetLoadInfo> filePathAccess)
        {
            if(filePathAccess != null)
            {
                loadPath = Utility.LeftSlash(loadPath);
                switch(GameConfig.Instance.resourceLoadMode)
                {
#if UNITY_EDITOR
                    case ResourcesLoadMode.AssetDataBase_Editor:
                        {
                            Utility.AccessEditorPath(loadPath, assetLoadInfo, filePathAccess);
                        }
                        break;
#endif
                    case ResourcesLoadMode.Resources:
                        {
                            filePathAccess.Invoke(loadPath, System.IO.Path.GetFileName(loadPath), assetLoadInfo);  // load a folder just use folder path in Resources Load Mode
                        }
                        break;
                    default:
                        {
                            if(Utility.CheckIsFolder(loadPath))
                            {
                                foreach(var assetFile in LocalVersion.Instance.versionInfo.AccessFilesEnumerable(loadPath, System.IO.SearchOption.TopDirectoryOnly))
                                {
                                    if(assetFile != null && assetFile.FinalFullNames != null && assetFile.FinalFullNames.Count > 0)
                                    {
                                        for(int i = 0; i < assetFile.FinalFullNames.Count; i++)
                                        {
                                            filePathAccess.Invoke(assetFile.FinalFullNames[i], assetFile.FileName, assetLoadInfo);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var assetFile = LocalVersion.Instance.versionInfo.SearchFile(loadPath);
                                if(assetFile != null && assetFile.FinalFullNames != null && assetFile.FinalFullNames.Count > 0)
                                {
                                    for(int i = 0, imax = assetFile.FinalFullNames.Count; i < imax; i++)
                                    {
                                        filePathAccess.Invoke(assetFile.FinalFullNames[i], assetFile.FileName, assetLoadInfo);
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// every level loader is a new loader
        /// </summary>
        /// <param name="levelLoadPath"></param>
        /// <param name="loadSceneMode"></param>
        /// <param name="loadMode"></param>
        /// <returns></returns>
        private LevelLoader GetLevelLoader(string levelLoadPath, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, LoadThreadMode loadMode, bool unloadable)
        {
            var levelName = System.IO.Path.GetFileNameWithoutExtension(levelLoadPath);
            LevelLoader levelLoader = null;
            AssetBundleLoader assetBundleLoader = null;
            var assetSource = GetAssetSource();
            if(configs.isBundleMode)
            {
                /* level loader is kind of special, on assetbundle mode, the scene name is an unique mark for assetbundle, it can't be load at the same time */
                var existsLoadPath = _uniqueLevelNames.TryGetValue(levelName);
                if(string.IsNullOrEmpty(existsLoadPath) == false && string.Equals(existsLoadPath, levelLoadPath, StringComparison.Ordinal) == false)
                {
                    _uniqueLevelNames.Remove(levelName);
                    // get old asset bundle loader
                    assetBundleLoader = _assetBundleRequests.TryGetValue(Utility.AssetPathToAssetBundleName(existsLoadPath));
                    if(assetBundleLoader != null)
                    {
                        if(assetBundleLoader.mainAssetBundle != null)
                        {
                            assetBundleLoader.mainAssetBundle.Release(true); // unload main Asset Bundle
                        }
                    }
                }

                var assetBundleName = Utility.AssetPathToAssetBundleName(levelLoadPath);
                assetBundleLoader = GetAssetBundleLoader(assetBundleName, loadMode, assetSource, unloadable);
                _uniqueLevelNames[levelName] = levelLoadPath;
            }

            // level loader is always new one
            levelLoader = new LevelLoader(levelLoadPath, levelName, loadSceneMode, assetSource, assetBundleLoader);
            _levelLoaderHashDict[levelLoader.GetHashCode()] = levelLoader;      // the Assets is Null
            // add every time, not unique
            var typeDatas = _levelLoaders.GetValue(levelLoadPath, () => { return TypeDataListPool.Allocate(); });
            var typeData = TypeDataPool.Allocate().Set(null, levelLoader);
            typeDatas.Add(typeData);

            return levelLoader;
        }

        /// <summary>
        /// Get asset bundle loader from cache or new
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="loadBundleMode"></param>
        /// <param name="assetSource"></param>
        /// <returns></returns>
        private AssetBundleLoader GetAssetBundleLoader(string assetBundleName, LoadThreadMode loadBundleMode, AssetSource assetSource, bool unloadable)
        {
            AssetBundleLoader assetBundleLoader = _assetBundleRequests.TryGetValue(assetBundleName);
            if(assetBundleLoader == null || assetBundleLoader.unloaded)
            {
                assetBundleLoader = new AssetBundleLoader(assetBundleName, assetSource);
                _assetBundleRequests[assetBundleName] = assetBundleLoader;
            }
            assetBundleLoader.unloadable &= unloadable;
            assetBundleLoader.LoadRequest(loadBundleMode);
            return assetBundleLoader;
        }

        /// <summary>
        /// load asset as unique in case we have multiple same named resource files
        /// </summary>
        /// <param name="assetLoadPath"></param>
        /// <param name="assetBundleName"></param>
        /// <param name="assetName"></param>
        /// <param name="systemTypeInstance"></param>
        /// <param name="loadMode"></param>
        /// <param name="isNew"></param>
        /// <returns></returns>
        private AssetLoader GetUniqueAssetLoader(string assetLoadPath,
            string assetBundleName,
            string assetName,
            System.Type systemTypeInstance,
            LoadThreadMode loadMode,
            bool unloadable)
        {
            string uniqueName = GenerateUniqueName(assetLoadPath, assetBundleName, assetName);
            var typeData = GetTypeData(_assetLoaders, uniqueName, systemTypeInstance);
            AssetLoader assetLoader = typeData;
            if((assetLoader == null || assetLoader.unloaded))
            {
                AssetBundleLoader assetBundleLoader = null;
                var assetSource = GetAssetSource();
                if(configs.isBundleMode)
                {
                    assetBundleLoader = GetAssetBundleLoader(assetBundleName, loadMode, assetSource, unloadable);
                }
                assetLoader = new AssetLoader(assetLoadPath, assetName, systemTypeInstance, assetSource, assetBundleLoader, false);
                typeData.Set(systemTypeInstance, assetLoader);
            }
            assetLoader.unloadable &= unloadable;
            return assetLoader;
        }

        /// <summary>
        /// Asset list to load
        /// </summary>
        /// <param name="assetList"></param>
        /// <param name="loadThreadMode"></param>
        /// <param name="loadAll"></param>
        /// <param name="loaded"></param>
        private void AssetListLoadRequest(AssetList assetList,
            LoadThreadMode loadThreadMode,
            bool loadAll,
            System.Action<UnityEngine.Object[]> loaded)
        {
            assetList.loadAll = loadAll;
            assetList.LoadRequest(loadThreadMode);
            if(loaded != null)
            {
                if(assetList.isDone)
                {
                    loaded.Invoke(assetList.Assets);
                }
                else
                {
                    assetList.completed.PushCall((_loader) =>
                    {
                        loaded.Invoke(_loader.Assets);
                    });
                }
            }
        }

        /// <summary>
        /// loaded asset changed, switch the reference
        /// </summary>
        /// <param name="assetList"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        private void OnAssetListAssetChanged(AssetList assetList, UnityEngine.Object[] from)
        {
            HashLog(assetList);
            if(from != null)
            {
                _assetLoaderHashDict.Remove(from.GetHashCode());
            }
        }

        /// <summary>
        /// Cache loader hash
        /// </summary>
        /// <param name="loader"></param>
        private void HashLog(AssetList loader)
        {
            if(loader != null && loader.Assets != null)
            {
                _assetLoaderHashDict[loader.Assets.GetHashCode()] = loader;
            }
        }

        /// <summary>
        /// make unique name from asset load path etc.
        /// </summary>
        /// <param name="assetLoadPath"></param>
        /// <param name="assetBundleName"></param>
        /// <param name="assetName"></param>
        /// <returns></returns>
        private string GenerateUniqueName(string assetLoadPath, string assetBundleName, string assetName)
        {
            _uniqueNameSB.Length = 0;
            _uniqueNameSB.Append(assetLoadPath);
            if(string.IsNullOrEmpty(assetBundleName) == false)
            {
                _uniqueNameSB.Append(":");
                _uniqueNameSB.Append(assetBundleName);
            }
            if(string.IsNullOrEmpty(assetName) == false)
            {
                _uniqueNameSB.Append(":");
                _uniqueNameSB.Append(assetName);
            }
            return _uniqueNameSB.ToString();
        }

        /// <summary>
        /// deferent asset mode has different asset sources
        /// </summary>
        /// <returns></returns>
        private AssetSource GetAssetSource()
        {
            switch(GameConfig.Instance.resourceLoadMode)
            {
                case ResourcesLoadMode.Resources:
                    {
                        return AssetSource.Resources;
                    }
#if UNITY_EDITOR
                case ResourcesLoadMode.AssetDataBase_Editor:
                    {
                        return AssetSource.AssetDataBase;
                    }
                case ResourcesLoadMode.AssetBundle_EditorTest:
#endif
                case ResourcesLoadMode.AssetBundle_StreamingAssets:
                case ResourcesLoadMode.AssetBundle_PersistentDataPath:
                    {
                        return AssetSource.LocalAssetBundle;
                    }
                case ResourcesLoadMode.AssetBundle_Remote:
                    {
                        return AssetSource.RemoteAssetBundle;
                    }
                default:
                    { return AssetSource.Resources; }
            }
        }

        /// <summary>
        /// Wrapped to get target type data
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="uniqueName"></param>
        /// <param name="systemTypeInstance"></param>
        /// <returns></returns>
        private TypeData GetTypeData(Dictionary<string, List<TypeData>> dict, string uniqueName, System.Type systemTypeInstance)
        {
            var typeData = dict.GetValue(uniqueName, () =>
            {
                return TypeDataListPool.Allocate();
            }).GetValue((_typeData) =>
            {
                return _typeData.type == systemTypeInstance;
            }, () =>
            {
                return TypeDataPool.Allocate();
            });
            return typeData;
        }
        #endregion

        #region Others
        private void LoadAssetBundleManifest()
        {
            if(configs.isRemoteAssets)
            {
                AssetBundleWebRequestController.Instance.MaxFailedCount = configs.downloadTimes;
                m_initWait = new Common.QueueCommands();
                EnqueueDownloadCommand(m_initWait, string.Concat(GameConfig.Instance.gameConfigSetting.RemoteURL, "/", typeof(AssetBundleManifest).Name), OnAssetBundleManifest);
                EnqueueDownloadCommand(m_initWait, string.Concat(GameConfig.Instance.gameConfigSetting.RemoteURL, "/", LocalVersion.VersionInfoBundleName), (_assetBundle) =>
                {
                    LocalVersion.Instance.versionInfo = LocalVersion.OnVersionInfoAssetBundle(_assetBundle);
                    Debug.Log("Current BundleVerison Version is : [" + LocalVersion.Instance.versionInfo.BundleVerison + "]");
                    CheckPrepared();
                });
            }
            else
            {
                var assetBundleManifestBundle = AssetBundle.LoadFromFile(Utility.AssetBundleNameToAssetBundlePath(GameConfig.AssetBundleManifestName, GameConfig.Instance.resourceLoadMode));
                OnAssetBundleManifest(assetBundleManifestBundle);
                Debug.Log("Current BundleVerison Version is : [" + LocalVersion.Instance.versionInfo.BundleVerison + "]");
                CheckPrepared();
            }
        }
        private void OnAssetBundleManifest(AssetBundle assetBundleManifestBundle)
        {
            if(assetBundleManifestBundle)
            {
                assetBundleManifest = assetBundleManifestBundle.LoadAsset<AssetBundleManifest>(GameConfig.AssetBundleManifestName);
                AssetBundleLoader.AssetBundleManifest = assetBundleManifest;
                assetBundleManifestBundle.Unload(false);
            }
        }

        private void EnqueueDownloadCommand(Common.QueueCommands queue, string url, System.Action<AssetBundle> access)
        {
            var download = new Command() { data = false, finishFunc = (_cmd) => { return (bool)_cmd.data; } };
            AssetBundleWebRequestController.Instance.DownloadAssetBundle(url, null,
                (_assetBundle) =>
                {
                    if(access != null)
                    {
                        access.Invoke(_assetBundle);
                    }
                    download.data = true;
                });
            queue.Enqueue(download);
        }
        private void CheckPrepared()
        {
            if(configs.isBundleMode)
            {
                Prepared = assetBundleManifest && LocalVersion.Instance.versionInfo != null;
            }
            else
            {
                Prepared = true;
            }
        }
        #endregion

        #region Auto Unload
        private void ClearRequest()
        {
            _unloadUnloadedLoaders = true;
            ms_assetUnloadCountDown.countDownTime = configs.isBundleMode ? unloadPaddingTime : 0.0f;     // if no assetBundle Mode, no need wait.
            ms_resourceUnloadCountDown.countDownTime = unloadPaddingTime;                        // call unload should always wait
        }
        private void RequestUnloadUnusedAssets()
        {
            ms_resourceUnloadCountDown.Start();
        }
        private void ClearUnusedAssetLoaders(Dictionary<string, List<TypeData>> dict)
        {
            if(dict.Count > 0)
            {
                dict.RemoveAll((_uniqueName, _typeDataList) =>
                {
                    if(_typeDataList.Count > 0)
                    {
                        _typeDataList.RemoveAll((_typeData) =>
                        {
                            if(_typeData == null)
                            {
                                return true;
                            }
                            bool unload = (_typeData.loader == null) || (_typeData.loader.unloaded);
                            if(unload)
                            {
                                TypeDataPool.DeAllocate(_typeData);
                            }
                            return unload;
                        });
                    }
                    // only main assets has no more used, should call unload func, especially important for None-AssetBundle mode
                    bool remove = _typeDataList.Count == 0;
                    if(remove)
                    {
                        if(ms_assetUnloadCountDown.started == false)
                        {
                            ms_assetUnloadCountDown.Start();
                        }
                        TypeDataListPool.DeAllocate(_typeDataList);
                    }
                    return remove;
                });
            }
        }
        private void TickUnferencedAssetBundleLoader()
        {
            if(_assetBundleRequests.Count > 0)
            {
                _assetBundleRequests.RemoveAll((_uniqueName, _assetBundleLoader) =>
                {
                    if(_assetBundleLoader == null)
                    {
                        return true;
                    }
                    if(_assetBundleLoader.mainAssetBundle != null && (_assetBundleLoader.mainAssetBundle.referenceCount <= 0 || _assetBundleLoader.unloaded))
                    {
                        if(_assetBundleLoader.Unload())
                        {
                            _unusedAssetCount += _assetBundleLoader.unmanagedAssetCount;
                        }
                        return true;
                    }
                    return false;
                });
            }
        }
        private void TickUnloadedLoaders()
        {
            _unloadUnloadedLoaders = false;
            ClearUnusedAssetLoaders(_assetLists);
            ClearUnusedAssetLoaders(_assetLoaders);
            ClearUnusedAssetLoaders(_levelLoaders);
        }
        private void UpdateUnload()
        {
            if(_unloadUnloadedLoaders)
            {
                TickUnloadedLoaders();
            }

            if(ms_assetUnloadCountDown.Finished())
            {
                if(configs.isBundleMode)
                {
                    TickUnferencedAssetBundleLoader();
                    RequestUnloadUnusedAssets();        // in AssetBundle mode, only when assetbundle unloaded, should call res unload
                }
                else
                {
                    RequestUnloadUnusedAssets();
                }
            }
            if(ms_resourceUnloadCountDown.Finished())
            {
                if(_unusedAssetCount > minUnloadAssetCounter)
                {
                    _unusedAssetCount = 0;
                    if(CoroutineRoot.Instance.IsCoroutineRunning(_unloadID) == false)
                    {
                        _unloadID = CoroutineRoot.Instance.StartCoroutineEx(UnloadUnusedAssets());
                    }
                }
            }

            if(configs.isBundleMode)
            {
                AssetLostUnload();
            }
        }

        private int _unloadID = CoroutineRoot.NULL;
        private IEnumerator UnloadUnusedAssets()
        {
            //System.GC.Collect(0);
            yield return new WaitForSeconds(unloadPaddingTime);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("Resources.UnloadUnusedAssets ...");
#endif
            ms_unloadedAssets.UnionWith(ms_unloadingAssets);
            ms_unloadingAssets.Clear();
            yield return Resources.UnloadUnusedAssets();
            if(onResourcesUnloaded != null)
            {
                onResourcesUnloaded.Invoke(ms_unloadedAssets);
            }
            ms_unloadedAssets.Clear();

            _unloadID = CoroutineRoot.NULL;
        }

        private void AssetLostUnload()
        {
            if(_unloadID != CoroutineRoot.NULL)
            {
                return;
            }
            if(m_inverseUnloadCountDown != null && m_inverseUnloadCountDown.Finished())
            {
                m_inverseUnloadCountDown.Start();
                AssetLoader.CheckUnload();
            }
        }
        #endregion

        private void Update()
        {
            UpdateUnload();
        }

    }
}

