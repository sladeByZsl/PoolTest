using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.Extention;

    public class VersionInfo : MonoBehaviour
    {
        [SerializeField]
        private string bundleVerison;
        [SerializeField]
        public string info;

        public string BundleVerison
        {
            get
            {
                return bundleVerison;
            }
#if UNITY_EDITOR
            set
            {
                bundleVerison = value;
            }
#endif
        }

        #region Main Funcs
        // search target file info
        public AssetFile SearchFile(string fileFullName)
        {
            string directory = null;
            string fileName = null;
            GetFileDirectoryAndName(fileFullName, out directory, out fileName);
            var assetFolder = transform.FindComponentInChild<AssetFolder>(directory);
            if(assetFolder)
            {
                return assetFolder.GetFile(fileName);
            }
            return null;
        }
        // access multi files
        public IEnumerable<AssetFile> AccessFilesEnumerable(string folderFullName, System.IO.SearchOption searchOption)
        {
            var tag = transform.Find(folderFullName);
            if(tag)
            {
                if(searchOption == System.IO.SearchOption.AllDirectories)
                {
                    var assetFolders = tag.GetComponentsInChildren<AssetFolder>();
                    if(assetFolders != null && assetFolders.Length > 0)
                    {
                        foreach(var assetFolder in assetFolders)
                        {
                            foreach(var assetFile in AccessFolderFilesEnumerable(assetFolder))
                            {
                                yield return assetFile;
                            }
                        }
                    }
                }
                else
                {
                    var assetFolder = tag.GetComponent<AssetFolder>();
                    if (assetFolder)
                    {
                        foreach(var assetFile in AccessFolderFilesEnumerable(assetFolder))
                        {
                            yield return assetFile;
                        }
                    }
                }
            }
        }
        // check the path is a folder
        public bool IsFolder(string folderFullName)
        {
            var tag = transform.Find(folderFullName);
            return tag;
        }
        #endregion

        #region Help Funcs
        private void GetFileDirectoryAndName(string fileFullName, out string directory, out string fileName)
        {
            directory = string.Empty;
            fileName = fileFullName;
            var index = Mathf.Max(fileFullName.LastIndexOf('/'), fileFullName.LastIndexOf('\\'));
            if(index > 0)
            {
                directory = fileFullName.Substring(0, index);
                fileName = fileFullName.Substring(index + 1);
            }
        }
        private IEnumerable<AssetFile> AccessFolderFilesEnumerable(AssetFolder assetFolder)
        {
            if(assetFolder && assetFolder.AssetFiles != null)
            {
                foreach(var data in assetFolder.AssetFiles)
                {
                    var assetFile = data.Value;
                    if(assetFile != null)
                    {
                        assetFile.FileName = data.Key;
                        yield return assetFile;
                    }
                }
            }
        }
        #endregion

#if UNITY_EDITOR
        #region Editor Funcs
        public AssetFile AddFileInfo(string relativePath, string finalFullName)
        {
            var fileInfo = RequireAssetFile(relativePath);
            if(fileInfo.FinalFullNames == null)
            {
                fileInfo.FinalFullNames = new List<string>();
            }
            if(fileInfo.FinalFullNames.Contains(finalFullName) == false)
            {
                fileInfo.FinalFullNames.Add(finalFullName);
            }
            return fileInfo;
        }
        private AssetFile RequireAssetFile(string relativePath)
        {
            var directory = Utility.GetDirectoryName(relativePath);
            var fileName = System.IO.Path.GetFileName(relativePath);
            var assetFolder = transform.RequireComponentInChild<AssetFolder>(directory);
            var assetFile = assetFolder.GetFile(fileName);
            if(assetFile == null)
            {
                assetFile = new AssetFile();
                if(assetFolder.AssetFiles == null)
                {
                    assetFolder.AssetFiles = new AssetFolder.AssetFileDictionary();
                }
                assetFolder.AssetFiles.Add(fileName, assetFile);
            }
            return assetFile;
        }
        #endregion
#endif

    }
}