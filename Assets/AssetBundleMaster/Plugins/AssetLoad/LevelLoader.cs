using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.Extention;
    using AssetBundleMaster.ContainerUtilities;

    public sealed class LevelLoader : AssetLoader
    {
        public const string LevelExt = ".unity";

        public string levelLoadPathExt { get; private set; }
        public string levelFullLoadPath { get; private set; }
        public string levelName { get { return assetName; } }
        public UnityEngine.SceneManagement.LoadSceneMode loadSceneMode { get; private set; }
        public Scene scene { get; private set; }
        public bool loadLevelStarted { get; private set; }    // if started, cant be stop

        // this global cache all level loaders that
        private static readonly Dictionary<string, WeakList<LevelLoader>> ms_sceneLoadedCalls = new Dictionary<string, WeakList<LevelLoader>>();

        private static Dictionary<Scene, LevelLoader> ms_loadedScenes = new Dictionary<Scene, LevelLoader>();
        public System.Action<Scene> onSceneUnloaded;

        // these BuildSetting scenes is the scenes added to BuildSettings for point out unique scene loading in Resources Mode
        private static readonly Dictionary<string, string> ms_allBuildSettingScenes = new Dictionary<string, string>();

        // static constructor
        static LevelLoader()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;

            // this take a little over head in editor mode
            if(GameConfig.Instance.resourceLoadMode == ResourcesLoadMode.Resources)
            {
                const string ResourcesFolderName = "Resources/";
                const string AssetFolderName = "Assets/";

                int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
                for(int i = 0; i < sceneCount; i++)
                {
                    var levelAssetPath = Utility.LeftSlash(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i));

                    // level name is not unique
                    string levelName = System.IO.Path.GetFileNameWithoutExtension(levelAssetPath);
                    if(ms_allBuildSettingScenes.ContainsKey(levelName) == false)
                    {
                        ms_allBuildSettingScenes[levelName] = levelAssetPath;
                    }

                    // relative is also not a unique path via Resources mode can have same file in diferent folder
                    string relativePath = levelAssetPath;
                    int indexResources = levelAssetPath.LastIndexOf(ResourcesFolderName);
                    if(indexResources >= 0)
                    {
                        relativePath = levelAssetPath.Substring(indexResources + ResourcesFolderName.Length);
                    }
                    if(ms_allBuildSettingScenes.ContainsKey(relativePath) == false)
                    {
                        ms_allBuildSettingScenes[relativePath] = levelAssetPath;
                    }

                    // scene path showing in BuildSettings window is unique
                    string buildSettingPath = levelAssetPath;
                    int indexAsset = levelAssetPath.IndexOf(AssetFolderName);
                    if(indexAsset >= 0)
                    {
                        buildSettingPath = levelAssetPath.Substring(indexAsset + AssetFolderName.Length);
                    }
                    if(ms_allBuildSettingScenes.ContainsKey(buildSettingPath) == false)
                    {
                        ms_allBuildSettingScenes[buildSettingPath] = levelAssetPath;
                    }
                }
            }
        }

        public LevelLoader(string loadPath, string levelName, LoadSceneMode loadSceneMode, AssetSource assetSource, AssetBundleLoader assetBundleLoader)
                : base(loadPath, levelName, typeof(UnityEngine.Object), assetSource, assetBundleLoader, true)
        {
            this.loadLevelStarted = false;
            this.loadSceneMode = loadSceneMode;
            this.levelLoadPathExt = this.loadPath.EndsWith(LevelExt) ? this.loadPath : (this.loadPath + LevelExt);
            this.levelFullLoadPath = levelLoadPathExt;

            switch(GameConfig.Instance.resourceLoadMode)
            {
                case ResourcesLoadMode.Resources:
                    {
                        this.levelFullLoadPath = this.loadPath;
                        var sceneLoadPath = ms_allBuildSettingScenes.TryGetValue(this.levelLoadPathExt) ?? ms_allBuildSettingScenes.TryGetValue(levelName);
                        if(string.IsNullOrEmpty(sceneLoadPath) == false)
                        {
                            this.levelFullLoadPath = sceneLoadPath;
                        }
                    }
                    break;
#if UNITY_EDITOR
                case ResourcesLoadMode.AssetBundle_EditorTest:
                case ResourcesLoadMode.AssetDataBase_Editor:
#endif
                case ResourcesLoadMode.AssetBundle_StreamingAssets:
                case ResourcesLoadMode.AssetBundle_PersistentDataPath:
                    {
                        this.levelFullLoadPath = string.Concat(GameConfig.Instance.assetBuildRoot, "/", this.levelLoadPathExt);
                    }
                    break;
            }
            ms_sceneLoadedCalls.GetValue(this.levelFullLoadPath, () =>
            {
                return ObjectPool.GlobalAllocator<WeakList<LevelLoader>>.Allocate();
            }).Add(this);
        }


        #region Main Funcs
        /// <summary>
        /// try to stop load level if not yet start loading
        /// </summary>
        /// <returns></returns>
        public bool StopLoadLevel()
        {
            if(false == loadLevelStarted)
            {
                RemoveLoadedCall();
                return true;
            }
            return false;
        }
        /// <summary>
        /// unload scene, we have 3 cases when to call unload a scene:
        /// 1. not yet start loading, this is only happen before assetbundle loaded
        /// 2. is loading scene, it can not be stoped but call unload scene after scene loaded
        /// 3. scene loaded, call unload scene
        /// </summary>
        /// <param name="unloadedCall"></param>
        public void UnloadScene(System.Action<Scene> unloadedCall = null)
        {
            if(unloadedCall != null)
            {
                onSceneUnloaded += unloadedCall;
            }
            StopLoadLevel();
            if(loadLevelStarted && isDone == false)
            {
                Debug.Log("UnloadScene : scene is in loading, unload will call after scene loaded. Frame : " + Time.frameCount);
                // is loading scene ...
                completed.PushCall((_loader) =>
                {
                    Unload();  // onSceneUnloaded call by global
                });
            }
            else
            {
                Unload();
                if(false == loadLevelStarted)
                {
                    if(onSceneUnloaded != null)
                    {
                        onSceneUnloaded.Invoke(new Scene());    // not yet start load scene, no scene info
                    }
                }
            }
        }
        #endregion

        #region Override Funcs
        public override void LoadRequest(LoadThreadMode loadAssetMode, ThreadPriority priority = ThreadPriority.Normal)
        {
            this.loadAssetMode = loadAssetMode;
            if(isDone == false)
            {
                if(assetBundleLoader != null)
                {
                    base.LoadRequest(loadAssetMode, priority);    // load asset bundle as assets
                }
                else
                {
#if UNITY_EDITOR
                    LoadLevel_Editor(this.levelFullLoadPath, this.loadSceneMode, loadAssetMode);
#else
                    LoadLevel(this.levelName, this.loadSceneMode, this.loadAssetMode);
#endif
                }
            }
        }
        protected override void OnAssetBundleLoaded()
        {
            if(assetBundleLoader != null && assetBundleLoader.isDone)
            {
                LoadLevel(this.levelName, this.loadSceneMode, this.loadAssetMode);
            }
        }
        public override bool Unload()
        {
            RemoveLoadedCall();
            if(isDone && scene.isLoaded && scene.IsValid())
            {
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            }
            base.Unload();
            return isDone;
        }
        public void Unload(bool unloadScene)
        {
            if(unloadScene)
            {
                Unload();
            }
            else
            {
                base.Unload();
            }
        }
        #endregion

        #region Help Funcs
        // load level in runtime, Notice: AssetBundle Mode don't need to add scenes to Buildsettings but Resources Mode need it
        private void LoadLevel(string levelName, LoadSceneMode loadSceneMode, LoadThreadMode loadMode)
        {
            this.loadLevelStarted = true;
            switch(GameConfig.Instance.resourceLoadMode)
            {
                case ResourcesLoadMode.Resources:
                    {
                        var index = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(levelFullLoadPath);
                        if(index >= 0)
                        {
                            switch(loadMode)
                            {
                                case LoadThreadMode.Synchronous:
                                    {
                                        UnityEngine.SceneManagement.SceneManager.LoadScene(index, loadSceneMode);
                                    }
                                    break;
                                case LoadThreadMode.Asynchronous:
                                    {
                                        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(index, loadSceneMode);  // the load cant be stop right here
                                    }
                                    break;
                            }
                        }
                    }
                    break;
                default:
                    {
                        switch(loadMode)
                        {
                            case LoadThreadMode.Synchronous:
                                {
                                    UnityEngine.SceneManagement.SceneManager.LoadScene(levelName, loadSceneMode);
                                }
                                break;
                            case LoadThreadMode.Asynchronous:
                                {
                                    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(levelName, loadSceneMode);  // the load cant be stop right here
                                }
                                break;
                        }
                    }
                    break;

            }
        }
#if UNITY_EDITOR
        // load level in editor mode, dont need to add scene to Buildsettings
        private void LoadLevel_Editor(string editorLevelLoadPath, LoadSceneMode loadSceneMode, LoadThreadMode loadMode)
        {
            this.loadLevelStarted = true;
            switch(loadSceneMode)
            {
                case UnityEngine.SceneManagement.LoadSceneMode.Single:
                    {
                        if(loadMode == LoadThreadMode.Synchronous)
                        {
#if UNITY_2018_1_OR_NEWER
                            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(editorLevelLoadPath, new LoadSceneParameters(LoadSceneMode.Single));
#else
                            UnityEditor.EditorApplication.LoadLevelInPlayMode(editorLevelLoadPath);
#endif
                        }
                        else
                        {
#if UNITY_2018_1_OR_NEWER
                            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(editorLevelLoadPath, new LoadSceneParameters(LoadSceneMode.Single));
#else
                            UnityEditor.EditorApplication.LoadLevelAsyncInPlayMode(editorLevelLoadPath);
#endif
                        }
                    }
                    break;
                case UnityEngine.SceneManagement.LoadSceneMode.Additive:
                    {
                        if(loadMode == LoadThreadMode.Synchronous)
                        {
#if UNITY_2018_1_OR_NEWER
                            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(editorLevelLoadPath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
                            UnityEditor.EditorApplication.LoadLevelAdditiveInPlayMode(editorLevelLoadPath);
#endif
                        }
                        else
                        {
#if UNITY_2018_1_OR_NEWER
                            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(editorLevelLoadPath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
                            UnityEditor.EditorApplication.LoadLevelAdditiveAsyncInPlayMode(editorLevelLoadPath);
#endif
                        }
                    }
                    break;
            }
        }
#endif
        // remove loaded calls in global
        private void RemoveLoadedCall()
        {
            var loadingList = ms_sceneLoadedCalls.TryGetValue(this.levelFullLoadPath);
            if(loadingList != null)
            {
                loadingList.Remove(this);
            }
        }

        // global scene loaded call
        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            WeakList<LevelLoader> levelLoaders = null;
            string sceneInfo = string.Empty;
            foreach(var data in ms_sceneLoadedCalls)
            {
                if(scene.path.EndsWith(data.Key))
                {
                    sceneInfo = data.Key;
                    levelLoaders = data.Value;
                    break;
                }
            }
            if(levelLoaders != null)
            {
                LevelLoader levelLoader = null;
                if(levelLoaders.Count > 0)
                {
                    levelLoader = levelLoaders[0];
                    levelLoaders.RemoveAt(0);
                    if(levelLoader != null)
                    {
                        levelLoader.scene = scene;
                        levelLoader.loadState = LoadState.Loaded;
                    }
                }
                if(levelLoaders.Count == 0)
                {
                    ms_sceneLoadedCalls.Remove(sceneInfo);
                    ObjectPool.GlobalAllocator<WeakList<LevelLoader>>.DeAllocate(levelLoaders);
                }
                ms_loadedScenes[scene] = levelLoader;
                if(levelLoader != null)
                {
                    levelLoader.Call();
                }
            }
        }
        // global scene unloaded call
        private static void OnSceneUnloaded(Scene scene)
        {
            var levelLoader = ms_loadedScenes.TryGetValue(scene);
            if(levelLoader != null)
            {
                ms_loadedScenes.Remove(scene);
                if(levelLoader.onSceneUnloaded != null)
                {
                    levelLoader.onSceneUnloaded.Invoke(scene);
                }
            }
        }
        #endregion        
    }
}

