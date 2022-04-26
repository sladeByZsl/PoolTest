using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace AssetBundleMaster.Editor
{
    using AssetBundleMaster.AssetLoad;
    using AssetBundleMaster.Extention;

    [CustomEditor(typeof(AssetLoadManager))]
    public class AssetLoadManagerInspector : UnityEditor.Editor
    {
        private AssetLoadManager _target = null;

        private Dictionary<string, AssetBundleLoader> _assetBundleRequests = null;

        private Dictionary<string, List<AssetLoadManager.TypeData>> _assetLoaders;
        private Dictionary<string, List<AssetLoadManager.TypeData>> _assetLists;
        private Dictionary<string, List<AssetLoadManager.TypeData>> _levelLoaders;

        private static Dictionary<string, bool> m_folders = new Dictionary<string, bool>();

        private static bool ms_debug = false;
        private static bool ms_filter = false;
        private static string ms_filterStr = string.Empty;

        private void OnEnable()
        {
            _target = this.target as AssetLoadManager;

            _assetBundleRequests = CommonEditorUtils.GetFieldValue<Dictionary<string, AssetBundleLoader>>(_target, "_assetBundleRequests");

            _assetLoaders = CommonEditorUtils.GetFieldValue<Dictionary<string, List<AssetLoadManager.TypeData>>>(_target, "_assetLoaders");
            _assetLists = CommonEditorUtils.GetFieldValue<Dictionary<string, List<AssetLoadManager.TypeData>>>(_target, "_assetLists");

            _levelLoaders = CommonEditorUtils.GetFieldValue<Dictionary<string, List<AssetLoadManager.TypeData>>>(_target, "_levelLoaders");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var oldColor = GUI.color;

            ms_debug = GUILayout.Toggle(ms_debug, "Debug : ");

            if(ms_debug)
            {
                _target.unloadPaddingTime = EditorGUILayout.FloatField("unloadPaddingTime : ", _target.unloadPaddingTime);
                _target.minUnloadAssetCounter = EditorGUILayout.IntField("minUnloadAssetCounter : ", _target.minUnloadAssetCounter);
            }

            GUILayout.Space(10.0f);
            if(_target.unloadAssetsWaitingTime > 0.0f)
            {
                EditorGUILayout.Slider("Unload Assets : " + _target.unloadAssetsWaitingTime.ToString("F2"), _target.unloadAssetsWaitingTime, 0, _target.unloadPaddingTime);
            }
            if(_target.unloadResourcesWaitingTime > 0.0f)
            {
                EditorGUILayout.Slider("Unload Resources : " + _target.unloadResourcesWaitingTime.ToString("F2"), _target.unloadResourcesWaitingTime, 0, _target.unloadPaddingTime);
            }

            GUILayout.Space(10.0f);
            if(_assetBundleRequests != null)
            {
                CommonEditorUtils.Foldout<string>(m_folders, "_assetBundleRequests", true, "Main Asset Bundles : " + _assetBundleRequests.Count, () =>
                {
                    foreach(var kv in _assetBundleRequests)
                    {
                        var isDone = CommonEditorUtils.GetPropertyValue<bool>(kv.Value, "isDone");
                        GUILayout.Label("AssetBundle : " + kv.Key);
                        GUILayout.Label("\tReferenceCount : " + kv.Value.mainAssetBundle.referenceCount + "  isDone : " + isDone);
                        GUILayout.Space(5.0f);
                    }
                });
            }

            GUILayout.Space(10.0f);
            if(_assetLoaders != null)
            {
                CommonEditorUtils.Foldout<string>(m_folders, "_assetLoaders", true, "Asset Requests : " + _assetLoaders.Count, () =>
                {
                    foreach(var kv in _assetLoaders)
                    {
                        GUI.color = Color.green;
                        GUILayout.Label("LoadInfo : " + kv.Key);
                        GUI.color = oldColor;
                        var Name = kv.Key;
                        foreach(var data in kv.Value)
                        {
                            var isDone = data.loader.isDone;        // CommonEditorUtils.GetPropertyValue<bool>(data.loader, "isDone");
                            var unloaded = data.loader.unloaded;    //CommonEditorUtils.GetPropertyValue<bool>(data.loader, "unloaded");
                            GUILayout.Label("       AssetLoader Type:[" + data.type.Name + "] | AssetName : " + (data.loader.assetName ?? "") + " | isDone : " + isDone + " | unloaded : " + unloaded);
                            var array = data.loader.Assets;         // CommonEditorUtils.GetPropertyValue<UnityEngine.Object[]>(data.loader, "Assets");
                            if(array != null && array.Length > 0)
                            {
                                foreach(var obj in array)
                                {
                                    if(obj)
                                    {
                                        GUILayout.Label("                   Loaded : " + obj);
                                    }
                                }
                            }
                        }
                        GUILayout.Space(5.0f);
                    }
                });
            }

            GUILayout.Space(10.0f);
            if(_assetLists != null)
            {
                CommonEditorUtils.Foldout<string>(m_folders, "_assetLists", true, "Asset Lists : " + _assetLists.Count, () =>
                {
                    foreach(var kv in _assetLists)
                    {
                        GUI.color = Color.green;
                        GUILayout.Label("LoadPath : " + kv.Key);
                        GUI.color = oldColor;
                        foreach(var data in kv.Value)
                        {
                            var isDone = CommonEditorUtils.GetPropertyValue<bool>(data.loader, "isDone");
                            var unloaded = CommonEditorUtils.GetPropertyValue<bool>(data.loader, "unloaded");
                            GUILayout.Label("       Type:" + data.type.Name + " | isDone : " + isDone + " | unloaded : " + unloaded);
                        }
                        GUILayout.Space(5.0f);
                    }
                });
            }

            GUILayout.Space(10.0f);
            if(_levelLoaders != null)
            {
                CommonEditorUtils.Foldout<string>(m_folders, "_levelLoaders", true, "Level Asset Loaders : " + _levelLoaders.Count, () =>
                {
                    foreach(var kv in _levelLoaders)
                    {
                        string sceneInfo = kv.Key;
                        var loadList = kv.Value;

                        GUI.color = Color.green;
                        GUILayout.Label("       Scene : " + sceneInfo);
                        GUI.color = oldColor;

                        CommonEditorUtils.Foldout<string>(m_folders, sceneInfo, true, "         Scenes", () =>
                        {
                            foreach(var loader in loadList)
                            {
                                var levelLoader = loader.loader as LevelLoader;
                                if(levelLoader != null)
                                {
                                    GUILayout.Label("               isDone : " + levelLoader.isDone + " | unloaded : " + levelLoader.unloaded);
                                }
                            }
                        });

                        GUILayout.Space(5.0f);
                    }
                });
            }

            GUILayout.Space(10.0f);
            var AssetBundleTargets = AssetBundleTarget.AssetBundleTargets.Select(_kv => _kv.Value).Where(_tag => _tag.loadState != LoadState.Ready).ToList();
            CommonEditorUtils.Foldout<string>(m_folders, "AssetBundleTargets", false, "AssetBundleTargets : " + AssetBundleTargets.Count, () =>
            {
                if(ms_debug)
                {
                    CommonEditorUtils.HorizontalLayout(() =>
                    {
                        ms_filter = GUILayout.Toggle(ms_filter, "Filter");
                        if(ms_filter)
                        {
                            ms_filterStr = EditorGUILayout.TextField("Filter : ", ms_filterStr);
                        }
                    });
                }
                foreach(var assetBundleTarget in AssetBundleTargets)
                {
                    if(ms_debug && ms_filter && string.IsNullOrEmpty(ms_filterStr) == false)
                    {
                        if(string.Equals(assetBundleTarget.assetName, ms_filterStr, StringComparison.OrdinalIgnoreCase) == false)
                        {
                            continue;
                        }
                    }

                    GUI.color = Color.green;
                    GUILayout.Label("AssetName : " + assetBundleTarget.assetName);
                    GUI.color = oldColor;

                    GUILayout.Label("       isDone : " + assetBundleTarget.isDone +
                        " | unloadable : " + assetBundleTarget.unloadable +
                        " | RefCount : " + assetBundleTarget.referenceCount +
                        " | IsMain : " + assetBundleTarget.isMain +
                        " | AssetLoadMode : " + assetBundleTarget.loadAssetMode);
                    GUILayout.Space(5.0f);

                    if(ms_debug)
                    {
                        if(GUILayout.Button("Release " + assetBundleTarget.assetName))
                        {
                            assetBundleTarget.Release(true);
                        }
                    }
                }
            });

            Repaint();
        }

    }
}