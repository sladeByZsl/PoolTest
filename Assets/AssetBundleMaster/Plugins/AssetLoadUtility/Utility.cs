using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace AssetBundleMaster.AssetLoad
{
    public static class Utility
    {
        private static readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder();

        #region AssetBundle Load Path
        // load path -> AssetBundle path
        public static string AssetPathToAssetBundleName(string assetLoadPath)
        {
            var assetBundleNames = AssetPathToAssetBundleNames(assetLoadPath);
            if(assetBundleNames != null && assetBundleNames.Count > 0)
            {
                return assetBundleNames[0];
            }
            var bundleName = (assetLoadPath + GameConfig.BundleDefaultExtName).ToLower();
            return bundleName;
        }
        // there are multi assets in multi assetbundles but has the same load path, this situation is a hell
        public static List<string> AssetPathToAssetBundleNames(string assetLoadPath)
        {
            List<string> assetBundleNames = null;
            if(LocalVersion.Instance.GetRedirectAssetBundleFullName(assetLoadPath, ref assetBundleNames))
            {
                if(assetBundleNames != null && assetBundleNames.Count > 0)
                {
                    return assetBundleNames;
                }
            }
            return assetBundleNames;
        }
        // AssetBundle Relative path -> AssetBundle Absolute path
        public static string AssetBundleNameToAssetBundlePath(string assetBundleName, ResourcesLoadMode resourcesLoadMode)
        {
            // Wrap Code, the asset path to asset bundle path should based on your project situation
            string bundleLoadPath = CombineAssetBundleLoadPath(assetBundleName, resourcesLoadMode);
            // persistentDataPath should be check, or load from streamingAssetsPath
            if(resourcesLoadMode == ResourcesLoadMode.AssetBundle_PersistentDataPath)
            {
                // bundleLoadPath in PersistentDataPath can be check
                if(System.IO.File.Exists(bundleLoadPath) == false)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("File No Exists : " + bundleLoadPath);
#endif

                    bundleLoadPath = CombineAssetBundleLoadPath(assetBundleName, ResourcesLoadMode.AssetBundle_StreamingAssets);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("Redirection Path To : " + bundleLoadPath);
#endif
                }
            }
            return bundleLoadPath;
        }
        #endregion

        #region Folder Path       
        public static string GetAssetLoadRoot(ResourcesLoadMode mode)
        {
            switch(mode)
            {
                case ResourcesLoadMode.AssetBundle_StreamingAssets:
                    {
                        return GameConfig.streamingAssetsPath.data;
                    }
                case ResourcesLoadMode.AssetBundle_PersistentDataPath:
                    {
                        return GameConfig.persistentDataPath.data;
                    }
                case ResourcesLoadMode.AssetBundle_Remote:
                    {
                        return GameConfig.Instance.gameConfigSetting.RemoteURL;
                    }
#if UNITY_EDITOR
                case ResourcesLoadMode.AssetDataBase_Editor:
                    {
                        return GameConfig.Instance.assetBuildRoot;   // editor path is changed ofen
                    }
                case ResourcesLoadMode.AssetBundle_EditorTest:
                    {
                        return GameConfig.getPlatformVersionFolder;   // editor path is changed ofen
                    }
#endif
            }
            return string.Empty;
        }

        public static string CombineAssetBundleLoadPath(string assetbundleName, ResourcesLoadMode resourcesLoadMode)
        {
            var root = GetAssetLoadRoot(resourcesLoadMode);
            if(string.IsNullOrEmpty(root))
            {
                return assetbundleName;
            }
            else
            {
                return string.Concat(root, "/", assetbundleName);
            }
        }

        public static bool CheckIsFolder(string path)
        {
            if(GameConfig.Instance.isBundleMode)
            {
                return LocalVersion.Instance.versionInfo.IsFolder(path);
            }
            else if(GameConfig.Instance.isResourcesMode)
            {
                return false;   // res mode don't care this check
            }
#if UNITY_EDITOR
            else if(GameConfig.Instance.isEditorMode)
            {
                var isFolder = System.IO.Directory.Exists(string.Concat(GameConfig.Instance.assetBuildRoot, "/", path));
                return isFolder;
            }
#endif
            return false;
        }

#if UNITY_EDITOR
        public static void AccessEditorPath(string loadPath, AssetLoadManager.AssetLoadInfo assetLoadInfo, System.Action<string, string, AssetLoadManager.AssetLoadInfo> filePathAccess)
        {
            if(filePathAccess != null)
            {
                if(CheckIsFolder(loadPath))
                {
                    Utility.GetEditorAssetLoadPathFromFolder(loadPath, assetLoadInfo, filePathAccess);
                }
                else
                {
                    var tagFileDir = string.Concat(GameConfig.Instance.assetBuildRoot, "/", loadPath);
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(loadPath);

                    if(System.IO.File.Exists(tagFileDir))
                    {
                        filePathAccess.Invoke(tagFileDir, fileName, assetLoadInfo);
                    }
                    else
                    {
                        Utility.GetEditorAssetLoadPathFromFolder(System.IO.Path.GetDirectoryName(loadPath), assetLoadInfo, filePathAccess, fileName + ".*");
                    }
                }
            }
        }
        public static void GetEditorAssetLoadPathFromFolder(string folderPath, AssetLoadManager.AssetLoadInfo assetLoadInfo, System.Action<string, string, AssetLoadManager.AssetLoadInfo> filePathAccess, string pattern = "*.*")
        {
            var tagFolder = GameConfig.Instance.assetBuildRoot;
            if(string.IsNullOrEmpty(folderPath) == false)
            {
                tagFolder = string.Concat(tagFolder, "/", folderPath);
            }
            var files = System.IO.Directory.GetFiles(tagFolder, pattern, SearchOption.TopDirectoryOnly);
            foreach(var file in files)
            {
                if(file.EndsWith(GameConfig.MetaFileExtName) == false)
                {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                    var loadPath = string.Concat(tagFolder, "/", System.IO.Path.GetFileName(file));
                    filePathAccess.Invoke(loadPath, fileName, assetLoadInfo);
                }
            }
        }
        public static string GetDirectoryName(string path)
        {
            return LeftSlash(System.IO.Path.GetDirectoryName(path));
        }
#endif
        #endregion

        #region Help Funcs
        /// <summary>
        /// use CreateInstance is the key to make target type array, and assetArray is the covariant of CreateInstance
        /// </summary>
        /// <param name="systemTypeInstance"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static UnityEngine.Object[] CreateAssetArray(System.Type systemTypeInstance, int length)
        {
            return System.Array.CreateInstance(systemTypeInstance, length) as UnityEngine.Object[];
        }

        public static string LeftSlash(string origin)
        {
            if(string.IsNullOrEmpty(origin))
            {
                return origin;
            }
            if(origin.LastIndexOf('\\') >= 0)
            {
                return origin.Replace('\\', '/');
            }
            return origin;
        }

        /// <summary>
        /// concat string, not thread safe
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <param name="str3"></param>
        /// <param name="str4"></param>
        /// <param name="str5"></param>
        /// <returns></returns>
        public static string StringConcat(string str1, string str2, string str3, string str4, string str5)
        {
            _sb.Length = 0;
            _sb.Append(str1);
            _sb.Append(str2);
            _sb.Append(str3);
            _sb.Append(str4);
            _sb.Append(str5);
            return _sb.ToString();
        }
        /// <summary>
        /// concat string, not thread safe
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <param name="str3"></param>
        /// <param name="str4"></param>
        /// <param name="str5"></param>
        /// <param name="str6"></param>
        /// <returns></returns>
        public static string StringConcat(string str1, string str2, string str3, string str4, string str5, string str6)
        {
            _sb.Length = 0;
            _sb.Append(str1);
            _sb.Append(str2);
            _sb.Append(str3);
            _sb.Append(str4);
            _sb.Append(str5);
            _sb.Append(str6);
            return _sb.ToString();
        }
        /// <summary>
        /// concat string, not thread safe
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <param name="str3"></param>
        /// <param name="str4"></param>
        /// <param name="str5"></param>
        /// <param name="str6"></param>
        /// <param name="str7"></param>
        /// <returns></returns>
        public static string StringConcat(string str1, string str2, string str3, string str4, string str5, string str6, string str7)
        {
            _sb.Length = 0;
            _sb.Append(str1);
            _sb.Append(str2);
            _sb.Append(str3);
            _sb.Append(str4);
            _sb.Append(str5);
            _sb.Append(str6);
            _sb.Append(str7);
            return _sb.ToString();
        }
        #endregion

    }

}