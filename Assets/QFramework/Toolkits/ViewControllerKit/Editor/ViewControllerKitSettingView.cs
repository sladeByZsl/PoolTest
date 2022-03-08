/****************************************************************************
 * Copyright (c) 2022.3 liangxiegame UNDER MIT LICENSE
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;
using UnityEditor;
using UnityEngine;

namespace QFramework
{
    public class ViewControllerKitSettingView
    {
        public void Init()
        {
        }
        
        private ViewControllerKitSettingLocale mLocaleText = new ViewControllerKitSettingLocale();

        public void OnGUI()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal();
                
                GUILayout.Label(mLocaleText.Setting, ViewControllerKitGUIStyles.Label12 , GUILayout.Width(200));
                
                GUILayout.FlexibleSpace();
                
                mLocaleText.CN = !GUILayout.Toggle(!mLocaleText.CN,"EN");
                
                GUILayout.EndHorizontal();

                GUILayout.Space(6);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(mLocaleText.Namespace, ViewControllerKitGUIStyles.LabelBold12 , GUILayout.Width(200));

                    ViewControllerKit.Setting.Namespace =
                        EditorGUILayout.TextField(ViewControllerKit.Setting.Namespace);
                }
                GUILayout.EndHorizontal();


                GUILayout.Space(12);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(mLocaleText.ViewControllerScriptGenerateDir, ViewControllerKitGUIStyles.LabelBold12,
                        GUILayout.Width(220));

                    ViewControllerKit.Setting.ScriptDir =
                        EditorGUILayout.TextField(ViewControllerKit.Setting.ScriptDir);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(mLocaleText.ViewControllerPrefabGenerateDir, ViewControllerKitGUIStyles.LabelBold12,
                        GUILayout.Width(220));
                    ViewControllerKit.Setting.PrefabDir =
                        EditorGUILayout.TextField(ViewControllerKit.Setting.PrefabDir);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);

                if (GUILayout.Button(mLocaleText.Apply))
                {
                    ViewControllerKit.Setting.Save();
                }
            }
            GUILayout.EndVertical();
        }

        public void OnDestroy()
        {
        }
    }

    public class ViewControllerKitSettingWindow : EditorWindow
    {
        [MenuItem("QFramework/Toolkits/ViewController Kit %#v")]
        public static void OpenWindow()
        {
            var window = (ViewControllerKitSettingWindow)GetWindow(typeof(ViewControllerKitSettingWindow), true);
            Debug.Log(Screen.width + " screen width*****");
            window.position = new Rect(100, 100, 600, 400);
            window.Show();
        }
        
        private void OnEnable()
        {
            mViewControllerKitSettingView = new ViewControllerKitSettingView();
            mViewControllerKitSettingView.Init();
        }

        ViewControllerKitSettingView mViewControllerKitSettingView = null;


        public void OnDisable()
        {
            mViewControllerKitSettingView.OnDestroy();
            mViewControllerKitSettingView = null;
        }

        public void OnGUI()
        {
            GUILayout.BeginVertical();

            mViewControllerKitSettingView.OnGUI();

            GUILayout.EndVertical();
            GUILayout.Space(50);
        }
    }
}