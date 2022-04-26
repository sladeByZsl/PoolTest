using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.ProjectWindowCallback;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;

namespace AssetBundleMaster.Editor
{
    using AssetBundleMaster.Extention;
    using AssetBundleMaster.Common;

    public class CommonEditorUtils
    {
        public static GUILayoutOption ms_commonButtonWidth_normal = GUILayout.Width(200);
        public static GUIStyle _GUIStyle_Tips;
        public static GUIStyle GUIStyle_Tips
        {
            get
            {
                if(_GUIStyle_Tips == null)
                {
                    _GUIStyle_Tips = new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = Color.green },
                        fontSize = 20
                    };
                }
                return _GUIStyle_Tips;
            }
        }

        public static readonly FixedStringData projectPathHeader = new FixedStringData(() =>
        { return CommonEditorUtils.LeftSlash(System.Environment.CurrentDirectory) + "/"; });

        #region Editor
        public static void SaveAndRefresh(ImportAssetOptions opt = ImportAssetOptions.Default)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(opt);
        }
        #endregion

        #region lay out Funcs / Editor Res
        public static bool MessageBox(string tips, string yes = "Yes", string cancel = "No")
        {
            return EditorUtility.DisplayDialog("MessageBox", tips, yes, cancel);
        }

        public static void VerticalLayout(System.Action layoutFunc, float? widthSet = null)
        {
            if(layoutFunc != null)
            {
                if(widthSet.HasValue)
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(widthSet.Value));
                }
                else
                {
                    EditorGUILayout.BeginVertical();
                }

                layoutFunc();
                EditorGUILayout.EndVertical();
            }
        }
        public static void HorizontalLayout(System.Action layoutFunc)
        {
            if(layoutFunc != null)
            {
                EditorGUILayout.BeginHorizontal();
                layoutFunc();
                EditorGUILayout.EndHorizontal();
            }
        }

        public static void ScrollViewLayout(ref Vector2 staticPos, System.Action layoutFunc)
        {
            if(layoutFunc != null)
            {
                staticPos = GUILayout.BeginScrollView(staticPos, false, true);
                layoutFunc();
                GUILayout.EndScrollView();
            }
        }
        public static void ScrollViewLayout(ref Vector2 staticPos, System.Action layoutFunc, float width, float height)
        {
            if(layoutFunc != null)
            {
                staticPos = GUILayout.BeginScrollView(staticPos, false, true, GUILayout.Width(width), GUILayout.Height(height));
                layoutFunc();
                GUILayout.EndScrollView();
            }
        }

        public static void DrawLine(string info = null)
        {
            GUILayout.Label(string.Format("---------------------{0}---------------------", info ?? string.Empty));
        }

        public static void Foldout<T>(Dictionary<T, bool> dict, T ins, bool defaultSet, string title, System.Action call)
        {
            var fold = GetUnfolderWrapped(dict, ins, defaultSet);
            var newState = EditorGUILayout.Foldout(fold, new GUIContent(title));
            if(newState != fold)
            {
                dict[ins] = newState;
            }
            if(newState)
            {
                if(call != null)
                {
                    call();
                }
            }
        }
        public static bool GetUnfolderWrapped<T>(Dictionary<T, bool> dict, T ins, bool defaultSet)
        {
            if(dict != null)
            {
                if(dict.ContainsKey(ins) == false)
                {
                    dict[ins] = defaultSet;
                }
            }
            return dict != null ? dict[ins] : defaultSet;
        }
        #endregion

        #region File Path Helper
        // left slash
        public static string LeftSlash(string src)
        {
            if(string.IsNullOrEmpty(src))
            {
                return src;
            }
            return src.Replace("\\", "/");
        }
        // full path to Asset/... path -- leftslash allready
        public static string FullPathToProjectPath(string fullName)
        {
            if(string.IsNullOrEmpty(fullName) == false)
            {
                fullName = LeftSlash(fullName);
                fullName = fullName.Replace(projectPathHeader, "");
                return fullName;
            }
            return string.Empty;
        }
        // Full Path to folder based load path, the extention was deleted
        public static string FullPathToResourceLoadPath(string fullPath, string loadFile)
        {
            fullPath = LeftSlash(fullPath);
            loadFile = LeftSlash(loadFile);
            int index = fullPath.IndexOf(loadFile);
            if(index >= 0)
            {
                var path = fullPath.Substring(index + loadFile.Length);
                if((index = path.LastIndexOf('.')) != -1)
                {
                    path = path.Substring(0, index);
                }
                while(path.StartsWith("/"))
                {
                    path = path.Substring(1);
                }
                return path;
            }
            return string.Empty;
        }
        // sub path from tag path
        public static string GetSubPath(string fullPath, string folder)
        {
            folder = LeftSlash(folder);
            fullPath = LeftSlash(fullPath);
            var index = fullPath.IndexOf(folder);
            if(index >= 0)
            {
                var path = fullPath.Substring(folder.Length);
                while(path.StartsWith("/"))
                {
                    path = path.Substring(1);
                }
                return path;
            }
            return fullPath;
        }
        // get a no extention path
        public static string StripExtension(string path)
        {
            int index = path.LastIndexOf('.');
            if(index != -1)
            {
                path = path.Substring(0, index);
            }
            return path;
        }

        // require folder
        public static bool RequireDirectory(string dir)
        {
            if(Directory.Exists(dir) == false)
            {
                return Directory.CreateDirectory(dir).Exists;
            }
            return true;
        }
        // simple get folder path
        public static string GetDirectoryName(string fullPath)
        {
            int index = fullPath.LastIndexOf("/");
            if(index > 0)
            {
                return fullPath.Substring(0, index);
            }
            return fullPath;
        }
        public static string FullPathWithoutExtension(string fullPath)
        {
            var retVal = "";
            var directory = System.IO.Path.GetDirectoryName(fullPath);
            if(string.IsNullOrEmpty(directory) == false)
            {
                retVal = directory + "/";
            }
            retVal += System.IO.Path.GetFileNameWithoutExtension(fullPath);
            return LeftSlash(retVal);
        }

        // get is totally empty?
        public static bool FastCheckDirectoryIsEmpty(string dir)
        {
            var files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
            if(files != null && files.Length > 0)
            {
                return false;
            }
            var children = Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly);
            if(children != null && children.Length > 0)
            {
                foreach(var child in children)
                {
                    if(FastCheckDirectoryIsEmpty(child) == false)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        #endregion

        #region Unity Resources Access
        /// <summary>
        /// Mainly used for reset bundle names
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public static bool SetAssetImporterInfo(string assetPath, string setBundleName, string bundleExt = null)
        {
            AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
            if(assetImporter)
            {
                assetImporter.assetBundleName = (setBundleName ?? string.Empty) + (bundleExt ?? string.Empty);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Delete Asset from project
        /// </summary>
        /// <param name="asset"></param>
        public static void DeleteAsset(UnityEngine.Object asset)
        {
            if(asset)
            {
                var loadPath = AssetDatabase.GetAssetPath(asset);
                if(string.IsNullOrEmpty(loadPath) == false)
                {
                    AssetDatabase.DeleteAsset(loadPath);
                }
            }
        }

        public static bool GetSelectionLoadPath(out string returnValue, ref bool isResource)
        {
            returnValue = string.Empty;
            if(Selection.activeObject)
            {
                bool succ = false;
                var assetBuildRoot = LeftSlash(AssetBundleMaster.AssetLoad.GameConfig.Instance.assetBuildRoot) + "/";
                var loadPath = LeftSlash(AssetDatabase.GetAssetPath(Selection.activeObject));
                returnValue = (loadPath);
                if(loadPath.Contains(assetBuildRoot))
                {
                    var relativePath = (loadPath.Replace(assetBuildRoot, ""));
                    returnValue = relativePath;
                    succ = true;
                    isResource = false;
                }
                else
                {
                    var resMark = "/Resources/";
                    int index = loadPath.LastIndexOf(resMark);
                    if(index > 0)
                    {
                        var relativePath = (loadPath.Substring(index + resMark.Length));
                        returnValue = relativePath;
                        succ = true;
                        isResource = true;
                    }
                }
                return succ;
            }
            return false;
        }
        #endregion

        #region Reflection
        public static T GetFieldValue<T>(object target, string fieldName)
        {
            var type = target.GetType();
            var fieldInfo = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
            try
            {
                var val = fieldInfo.GetValue(target);
                return (T)val;
            }
            catch(System.Exception ex)
            {
                Debug.LogError(@ex.ToString());
                return default(T);
            }
        }
        public static T GetPropertyValue<T>(object target, string propName)
        {
            var type = target.GetType();
            var propInfo = type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);
            try
            {
                var val = propInfo.GetValue(target, null);
                return (T)val;
            }
            catch(System.Exception ex)
            {
                Debug.LogError(@ex.ToString());
                return default(T);
            }
        }
        #endregion

        #region Timer
        public class ObsoluteTime
        {
            // ticks
            public static long TimeNow_Ticks
            {
                get
                {
                    return System.DateTime.Now.Ticks;
                }
            }
            // ms
            public static long TimeNow_Millisecond
            {
                get
                {
                    return DateTime.Now.Ticks / 10000;
                }
            }

            public static long GetMillisecond(long lastTick)
            {
                return (DateTime.Now.Ticks - lastTick) / 10000;
            }

            public long timeNow_Ticks = 0;
            public void Start()
            {
                timeNow_Ticks = TimeNow_Ticks;
            }
            public long GetMillisecond()
            {
                var mm = GetMillisecond(timeNow_Ticks);
                timeNow_Ticks = TimeNow_Ticks;
                return mm;
            }
            public float GetSeconds()
            {
                var mm = GetMillisecond();
                return ((float)mm) * 0.001f;
            }
        }
        #endregion

        #region Menu Item
        [MenuItem("Assets/AssetBundleMaster Tools/Copy Asset Load Path", false, 0)]
        public static void CopyLoadPath()
        {
            string loadPath = null;
            bool isRes = false;
            if(GetSelectionLoadPath(out loadPath, ref isRes))
            {
                GUIUtility.systemCopyBuffer = loadPath;
                Debug.Log(GUIUtility.systemCopyBuffer);
            }
            else
            {
                Debug.LogError("Has no load path");
                if(Selection.activeObject)
                {
                    GUIUtility.systemCopyBuffer = LeftSlash(AssetDatabase.GetAssetPath(Selection.activeObject));
                    Debug.Log("Get Asset Load Path : " + GUIUtility.systemCopyBuffer);
                }
            }
        }


        [MenuItem("Assets/AssetBundleMaster Tools/Asset/Load/Get Load Asset Code", false, 101)]
        public static void GetLoadAssetCode()
        {
            const string format_assetRoot = "var {1}_asset = AssetBundleMaster.ResourceLoad.ResourceLoadManager.Instance.Load<UnityEngine.Object>(\"{0}\");";
            const string format_resources = "var {1}_asset = UnityEngine.Resources.Load<UnityEngine.Object>(\"{0}\");";
            string loadPath = null;
            bool isRes = false;
            string fianlPath = "";
            string assetName = "";
            if(GetSelectionLoadPath(out loadPath, ref isRes))
            {
                assetName = System.IO.Path.GetFileNameWithoutExtension(loadPath);
                if(isRes)
                {
                    fianlPath = FullPathWithoutExtension(loadPath);
                }
                else
                {
                    fianlPath = loadPath;
                }
            }
            GUIUtility.systemCopyBuffer = isRes ? string.Format(format_resources, fianlPath, assetName) : string.Format(format_assetRoot, fianlPath, assetName);
            Debug.Log(GUIUtility.systemCopyBuffer);
        }
        [MenuItem("Assets/AssetBundleMaster Tools/Asset/Load/Get Load Asset Code(Async)", false, 102)]
        public static void GetLoadAssetAsyncCode()
        {
            const string format_assetRoot = "AssetBundleMaster.ResourceLoad.ResourceLoadManager.Instance.LoadAsync<UnityEngine.Object>(\"{0}\", ({1}_asset)=>{{ }});";
            const string format_resources = "var {1}_request = UnityEngine.Resources.LoadAsync<UnityEngine.Object>(\"{0}\");";
            string loadPath = null;
            bool isRes = false;
            string fianlPath = "";
            string assetName = "";
            if(GetSelectionLoadPath(out loadPath, ref isRes))
            {
                assetName = System.IO.Path.GetFileNameWithoutExtension(loadPath);
                if(isRes)
                {
                    fianlPath = FullPathWithoutExtension(loadPath);
                }
                else
                {
                    fianlPath = loadPath;
                }
            }
            GUIUtility.systemCopyBuffer = isRes ? string.Format(format_resources, fianlPath, assetName) : string.Format(format_assetRoot, fianlPath, assetName);
            Debug.Log(GUIUtility.systemCopyBuffer);
        }
        [MenuItem("Assets/AssetBundleMaster Tools/Asset/Load/Get LoadAll Assets Code", false, 103)]
        public static void GetLoadAllAssetsCode()
        {
            const string format_assetRoot = "var {1}_asset = AssetBundleMaster.ResourceLoad.ResourceLoadManager.Instance.LoadAll<UnityEngine.Object>(\"{0}\");";
            const string format_resources = "var {1}_asset = UnityEngine.Resources.LoadAll<UnityEngine.Object>(\"{0}\");";
            string loadPath = null;
            bool isRes = false;
            string fianlPath = "";
            string assetName = "";
            if(GetSelectionLoadPath(out loadPath, ref isRes))
            {
                assetName = System.IO.Path.GetFileNameWithoutExtension(loadPath);
                if(isRes)
                {
                    fianlPath = FullPathWithoutExtension(loadPath);
                }
                else
                {
                    fianlPath = loadPath;
                }
            }
            GUIUtility.systemCopyBuffer = isRes ? string.Format(format_resources, fianlPath, assetName) : string.Format(format_assetRoot, fianlPath, assetName);
            Debug.Log(GUIUtility.systemCopyBuffer);
        }
        [MenuItem("Assets/AssetBundleMaster Tools/Asset/Load/Get LoadAll Assets Code(Async)", false, 104)]
        public static void GetLoadAllAssetsAsyncCode()
        {
            const string format_assetRoot = "AssetBundleMaster.ResourceLoad.ResourceLoadManager.Instance.LoadAllAsync<UnityEngine.Object>(\"{0}\", ({1}_assets)=>{{ }});";
            const string format_resources = "var {1}_request = UnityEngine.Resources.LoadAll<UnityEngine.Object>(\"{0}\");";
            string loadPath = null;
            bool isRes = false;
            string fianlPath = "";
            string assetName = "";
            if(GetSelectionLoadPath(out loadPath, ref isRes))
            {
                assetName = System.IO.Path.GetFileNameWithoutExtension(loadPath);
                if(isRes)
                {
                    fianlPath = FullPathWithoutExtension(loadPath);
                }
                else
                {
                    fianlPath = loadPath;
                }
            }
            GUIUtility.systemCopyBuffer = isRes ? string.Format(format_resources, fianlPath, assetName) : string.Format(format_assetRoot, StripExtension(fianlPath), assetName);
            Debug.Log(GUIUtility.systemCopyBuffer);
        }
        [MenuItem("Assets/AssetBundleMaster Tools/Asset/Unload/Get UnLoad Asset Code", false, 105)]
        public static void GetUnLoadAssetCode()
        {
            const string format_assetRoot = "AssetBundleMaster.ResourceLoad.ResourceLoadManager.Instance.UnloadAsset<UnityEngine.Object>(\"{0}\", true);";
            string loadPath = null;
            bool isRes = false;
            string fianlPath = "";
            if(GetSelectionLoadPath(out loadPath, ref isRes))
            {
                if(false == isRes)
                {
                    fianlPath = loadPath;
                }
            }
            GUIUtility.systemCopyBuffer = string.Format(format_assetRoot, fianlPath);
            Debug.Log(GUIUtility.systemCopyBuffer);
        }


        [MenuItem("Assets/AssetBundleMaster Tools/GameObject/Spawn/Get Spawn GameObject Code", false, 201)]
        public static void GetSpawnGameObjectCode()
        {
            const string format_assetRoot = "var {1}_go = AssetBundleMaster.ResourceLoad.PrefabLoadManager.Instance.Spawn(\"{0}\");";
            string loadPath = null;
            bool isRes = false;
            string fianlPath = "";
            string assetName = "";
            if(GetSelectionLoadPath(out loadPath, ref isRes) && false == isRes && loadPath.EndsWith(".prefab"))
            {
                assetName = System.IO.Path.GetFileNameWithoutExtension(loadPath);
                fianlPath = loadPath;
            }
            GUIUtility.systemCopyBuffer = string.Format(format_assetRoot, fianlPath, assetName);
            Debug.Log(GUIUtility.systemCopyBuffer);
        }
        [MenuItem("Assets/AssetBundleMaster Tools/GameObject/Spawn/Get Spawn GameObject Code(Async)", false, 202)]
        public static void GetSpawnGameObjectAsyncCode()
        {
            const string format_assetRoot = "AssetBundleMaster.ResourceLoad.PrefabLoadManager.Instance.SpawnAsync(\"{0}\", ({1}_go)=>{{ }});";
            string loadPath = null;
            bool isRes = false;
            string fianlPath = "";
            string assetName = "";
            if(GetSelectionLoadPath(out loadPath, ref isRes) && false == isRes && loadPath.EndsWith(".prefab"))
            {
                assetName = System.IO.Path.GetFileNameWithoutExtension(loadPath);
                fianlPath = loadPath;
            }
            GUIUtility.systemCopyBuffer = string.Format(format_assetRoot, fianlPath, assetName);
            Debug.Log(GUIUtility.systemCopyBuffer);
        }
        [MenuItem("Assets/AssetBundleMaster Tools/GameObject/Despawn/Get DeSpawn Code", false, 203)]
        public static void GetDespawnCode()
        {
            const string format_assetRoot = "AssetBundleMaster.ResourceLoad.PrefabLoadManager.Instance.Despawn(spawned);";
            GUIUtility.systemCopyBuffer = format_assetRoot;
            Debug.Log(GUIUtility.systemCopyBuffer);
        }
        [MenuItem("Assets/AssetBundleMaster Tools/GameObject/Destroy/Get DestroySpawned Code", false, 204)]
        public static void GetDestroySpawnedCode()
        {
            const string format_assetRoot = "AssetBundleMaster.ResourceLoad.PrefabLoadManager.Instance.DestroySpawned(spawned);";
            GUIUtility.systemCopyBuffer = format_assetRoot;
            Debug.Log(GUIUtility.systemCopyBuffer);
        }


        [MenuItem("Assets/AssetBundleMaster Tools/Scene/Load/Get Scene Load Code", false, 301)]
        public static void GetSceneLoadCode()
        {
            const string format_assetRoot = @"var id = AssetBundleMaster.ResourceLoad.SceneLoadManager.Instance.LoadScene(""#loadPath#"",
    AssetBundleMaster.AssetLoad.LoadThreadMode.Asynchronous,
    UnityEngine.SceneManagement.LoadSceneMode.Additive,
    (_id, _scene) =>
    {
        // scene loaded call back, id == _id is runtime hashcode
    }); ";
            string loadPath = null;
            bool isRes = false;
            string fianlPath = "";
            if(GetSelectionLoadPath(out loadPath, ref isRes) && false == isRes && loadPath.EndsWith(".unity"))
            {
                fianlPath = loadPath;
            }
            GUIUtility.systemCopyBuffer = format_assetRoot.Replace("#loadPath#", fianlPath);
            Debug.Log(GUIUtility.systemCopyBuffer);
        }
        [MenuItem("Assets/AssetBundleMaster Tools/Scene/Unload/Get Scene UnLoad Code", false, 302)]
        public static void GetSceneUnLoadCode()
        {
            const string format_assetRoot = @"AssetBundleMaster.ResourceLoad.SceneLoadManager.Instance.UnloadScene(id,
    (_id, _scene) =>
    {
        // scene unloaded call back, id == _id is runtime hashcode
    }); ";
            GUIUtility.systemCopyBuffer = format_assetRoot;
            Debug.Log(GUIUtility.systemCopyBuffer);
        }


        //[MenuItem("Test/Test")]
        //public static void Test()
        //{
        //}
        #endregion

    }
}