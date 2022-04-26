using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;


namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.Common;
    using AssetBundleMaster.ContainerUtilities;

    /// <summary>
    /// the local version info struct
    /// </summary>
    public class LocalVersion : Singleton<LocalVersion>
    {
        #region Defines
        public class UpdateInfo : BaseJsonSerialization
        {
            [SerializeField]
            public string buildTime;

            public System.Version from;
            public System.Version to;

            [SerializeField]
            public List<string> updateList = new List<string>();
            [SerializeField]
            public List<string> deleteList = new List<string>();
        }
        #endregion

        public const string BundleVersionFileName = "VersionInfo";
        public const string BundleVersionKey = "BundleVersion";
        public const string BundleName = "versioninfo";

        public static string VersionInfoBundleName = BundleName + GameConfig.BundleDefaultExtName;
        public VersionInfo versionInfo = null;

        protected override void Initialize()
        {
            if(Application.isPlaying)
            {
                LoadInPlaying();
            }
        }
        protected override void UnInitialize()
        {
        }

        #region Main Func
        private void LoadInPlaying()
        {
            var loadMode = GameConfig.Instance.resourceLoadMode;
            // no remote mode
            switch(loadMode)
            {
#if UNITY_EDITOR
                case ResourcesLoadMode.AssetBundle_EditorTest:
#endif
                case ResourcesLoadMode.AssetBundle_StreamingAssets:
                case ResourcesLoadMode.AssetBundle_PersistentDataPath:
                    {
                        versionInfo = LoadVersionInfo(loadMode);
                    }
                    break;
            }
        }

        public VersionInfo LoadVersionInfo(ResourcesLoadMode resourcesLoadMode)
        {
            string localVersionFilePath = Utility.AssetBundleNameToAssetBundlePath(VersionInfoBundleName, resourcesLoadMode);
            var assetBundle = AssetBundle.LoadFromFile(localVersionFilePath);
            return OnVersionInfoAssetBundle(assetBundle);
        }
        public static VersionInfo OnVersionInfoAssetBundle(AssetBundle assetBundle)
        {
            VersionInfo versionInfo = null;
            if(assetBundle)
            {
                var localVersionAsset = assetBundle.LoadAsset<GameObject>(BundleVersionFileName);
                if(localVersionAsset)
                {
                    versionInfo = localVersionAsset.GetComponent<VersionInfo>();
                }
                assetBundle.Unload(false);
            }
            return versionInfo;
        }
        #endregion

        public bool GetRedirectAssetBundleFullName(string loadPath, ref List<string> assetBundleNames)
        {
            if(versionInfo != null)
            {
                var file = versionInfo.SearchFile(loadPath);
                if(file != null && file.FinalFullNames.Count > 0)
                {
                    assetBundleNames = file.FinalFullNames;
                    return true;
                }
            }
            else
            {
                Debug.LogError("You need to wait AssetLoadManager inited, plwase see AssetLoadManager.OnAssetLoadModuleInited function");
            }
            return false;
        }
    }
}


