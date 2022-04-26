using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.Common;

    [System.Serializable]
    public class GameConfigSetting : CustomScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField]
        public ResourcesLoadMode ResourcesLoadModeEditor;
#endif

        [Space(10.0f)]
        [SerializeField]
        public ResourcesLoadMode ResourcesLoadModeRuntime;
        [Space(10.0f)]
        [SerializeField]
        public string RemoteURL;
        [Space(10.0f)]
        [SerializeField]
        public int DownloadTryTimes = 5;
        [Space(10.0f)]
        [SerializeField]
        public string Version;
        [Space(10.0f)]
        [SerializeField]
        public string assetBuildRoot;

        public System.Version _version;
        public System.Version version
        {
            get
            {
                if(_version == null)
                {
                    _version = new System.Version(Version);
                }
                return _version;
            }
        }
    }
}