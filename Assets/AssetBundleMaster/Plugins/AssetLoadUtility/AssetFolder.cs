using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.Extention;
    using AssetBundleMaster.Common;

    public class AssetFolder : MonoBehaviour
    {
        // for serialization
        [System.Serializable]
        public class AssetFileDictionary : SerialiserableDictionary<string, AssetFile> { }

        [SerializeField]
        public AssetFileDictionary AssetFiles = null;

        public AssetFile GetFile(string fileName)
        {
            if(AssetFiles != null)
            {
                var assetFile = AssetFiles.TryGetValue(fileName);
                if(assetFile != null)
                {
                    assetFile.FileName = fileName;
                }
                return assetFile;
            }
            return null;
        }
    }
}