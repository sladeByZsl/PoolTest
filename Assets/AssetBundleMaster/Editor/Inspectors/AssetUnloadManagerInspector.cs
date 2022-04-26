using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace AssetBundleMaster.Editor
{
    using AssetBundleMaster.AssetLoad;
    using AssetBundleMaster.Extention;

    [CustomEditor(typeof(AssetUnloadManager))]
    public class AssetUnloadManagerInspector : UnityEditor.Editor
    {
        private AssetUnloadManager _target = null;

        private void OnEnable()
        {
            _target = this.target as AssetUnloadManager;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            for(int i = 0; i < _target.unloadingAssetBundles.Count; i++)
            {
                var item = _target.unloadingAssetBundles[i];
                GUILayout.Label(string.Concat(item.assetBundleTarget.assetName, ":", item.unloadLogic));
            }

            Repaint();
        }

        private void OnSceneGUI()
        {
            Repaint();
        }
    }
}