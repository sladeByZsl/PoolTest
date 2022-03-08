/****************************************************************************
 * Copyright (c) 2017 xiaojun
 * Copyright (c) 2017 ~ 2022.3  liangxiegame
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QFramework
{
    [CustomEditor(typeof(ViewController), true)]
    public class ViewControllerInspector : Editor
    {
        private ViewControllerInspectorLocale mLocaleText = new ViewControllerInspectorLocale();

        private ViewController mCodeGenerateInfo
        {
            get { return target as ViewController; }
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(mCodeGenerateInfo.ScriptsFolder))
            {
                mCodeGenerateInfo.ScriptsFolder = ViewControllerKit.Setting.ScriptDir;
            }

            if (string.IsNullOrEmpty(mCodeGenerateInfo.PrefabFolder))
            {
                mCodeGenerateInfo.PrefabFolder = ViewControllerKit.Setting.PrefabDir;
            }

            if (string.IsNullOrEmpty(mCodeGenerateInfo.ScriptName))
            {
                mCodeGenerateInfo.ScriptName = mCodeGenerateInfo.name;
            }

            if (string.IsNullOrEmpty(mCodeGenerateInfo.Namespace))
            {
                mCodeGenerateInfo.Namespace = ViewControllerKit.Setting.Namespace;
            }
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            GUILayout.Label(mLocaleText.CodegenPart, new GUIStyle()
            {
                fontStyle = FontStyle.Bold,
                fontSize = 15
            });
            
            GUILayout.FlexibleSpace();
            mLocaleText.CN = !GUILayout.Toggle(!mLocaleText.CN,"EN");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(mLocaleText.Namespace, GUILayout.Width(150));
            mCodeGenerateInfo.Namespace = EditorGUILayout.TextArea(mCodeGenerateInfo.Namespace);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(mLocaleText.ScriptName, GUILayout.Width(150));
            mCodeGenerateInfo.ScriptName = EditorGUILayout.TextArea(mCodeGenerateInfo.ScriptName);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(mLocaleText.ScriptsFolder, GUILayout.Width(150));
            mCodeGenerateInfo.ScriptsFolder =
                EditorGUILayout.TextArea(mCodeGenerateInfo.ScriptsFolder, GUILayout.Height(30));

            GUILayout.EndHorizontal();


            EditorGUILayout.Space();
            EditorGUILayout.LabelField(mLocaleText.DragDescription);
            var sfxPathRect = EditorGUILayout.GetControlRect();
            sfxPathRect.height = 200;
            GUI.Box(sfxPathRect, string.Empty);
            EditorGUILayout.LabelField(string.Empty, GUILayout.Height(185));
            if (
                Event.current.type == EventType.DragUpdated
                && sfxPathRect.Contains(Event.current.mousePosition)
            )
            {
                //改变鼠标的外表  
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    if (DragAndDrop.paths[0] != "")
                    {
                        var newPath = DragAndDrop.paths[0];
                        mCodeGenerateInfo.ScriptsFolder = newPath;
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    }
                }
            }


            GUILayout.BeginHorizontal();
            mCodeGenerateInfo.GeneratePrefab =
                GUILayout.Toggle(mCodeGenerateInfo.GeneratePrefab, mLocaleText.GeneratePrefab);
            GUILayout.EndHorizontal();

            if (mCodeGenerateInfo.GeneratePrefab)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(mLocaleText.PrefabGenerateFolder, GUILayout.Width(150));
                mCodeGenerateInfo.PrefabFolder =
                    GUILayout.TextArea(mCodeGenerateInfo.PrefabFolder, GUILayout.Height(30));
                GUILayout.EndHorizontal();
            }

            var fileFullPath = mCodeGenerateInfo.ScriptsFolder + "/" + mCodeGenerateInfo.ScriptName + ".cs";
            if (File.Exists(mCodeGenerateInfo.ScriptsFolder + "/" + mCodeGenerateInfo.ScriptName + ".cs"))
            {
                var scriptObject = AssetDatabase.LoadAssetAtPath<MonoScript>(fileFullPath);
                if (GUILayout.Button(mLocaleText.OpenScript, GUILayout.Height(30)))
                {
                    AssetDatabase.OpenAsset(scriptObject);
                }

                if (GUILayout.Button(mLocaleText.SelectScript, GUILayout.Height(30)))
                {
                    Selection.objects = new Object[] {scriptObject};
                }
            }

            if (GUILayout.Button(mLocaleText.Generate, GUILayout.Height(30)))
            {
                CreateViewControllerCode.DoCreateCodeFromScene(((ViewController) target).gameObject);
                GUIUtility.ExitGUI();
            }

            GUILayout.EndVertical();
        }
    }
}