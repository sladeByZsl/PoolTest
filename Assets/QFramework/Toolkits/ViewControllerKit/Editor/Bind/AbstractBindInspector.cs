/****************************************************************************
 * Copyright (c) 2017 ~ 2022.3 liangxiegame
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QFramework
{

    [CustomEditor(typeof(AbstractBind), true)]
    public class AbstractBindInspector : UnityEditor.Editor
    {
        private BindInspectorLocale mLocaleText = new BindInspectorLocale();

        private AbstractBind mBindScript
        {
            get { return target as AbstractBind; }
        }


        private string[] mComponentNames;
        private int mComponentNameIndex;

        private void OnEnable()
        {
            var components = mBindScript.GetComponents<Component>();

            mComponentNames = components.Where(c => !(c is AbstractBind))
                .Select(c => c.GetType().FullName)
                .ToArray();

            mComponentNameIndex = mComponentNames.ToList()
                .FindIndex((componentName) => componentName.Contains(mBindScript.Name));

            if (mComponentNameIndex == -1 || mComponentNameIndex >= mComponentNames.Length)
            {
                mComponentNameIndex = 0;
            }
        }

        private Lazy<GUIStyle> mLabel12 = new Lazy<GUIStyle>(() => new GUIStyle(GUI.skin.label)
        {
            fontSize = 12
        });

        public override void OnInspectorGUI()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label(mLocaleText.Bind, new GUIStyle()
            {
                fontStyle = FontStyle.Bold,
                fontSize = 15
            });
            GUILayout.FlexibleSpace();
            mLocaleText.CN = !GUILayout.Toggle(!mLocaleText.CN, "EN");
            GUILayout.EndHorizontal();
            GUILayout.Space(10);


            GUILayout.BeginHorizontal();
            GUILayout.Label(mLocaleText.Type, mLabel12.Value, GUILayout.Width(60));
            mComponentNameIndex = EditorGUILayout.Popup(mComponentNameIndex, mComponentNames);
            mBindScript.Name = mComponentNames[mComponentNameIndex];
            GUILayout.EndHorizontal();


            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label(mLocaleText.BelongsTo, mLabel12.Value, GUILayout.Width(60));

            GUILayout.Label(CodeGenUtil.GetBindBelongs2(mBindScript), mLabel12.Value, GUILayout.Width(200));

            if (GUILayout.Button(mLocaleText.Select, GUILayout.Width(60)))
            {
                Selection.objects = new Object[]
                {
                    CodeGenUtil.GetBindBelongs2GameObject(target as AbstractBind)
                };
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.Label(mLocaleText.Comment, mLabel12.Value);

            GUILayout.Space(10);

            mBindScript.CustomComment = EditorGUILayout.TextArea(mBindScript.Comment, GUILayout.Height(100));

            var rootGameObj = CodeGenUtil.GetBindBelongs2GameObject(mBindScript);


            if (rootGameObj.transform.IsViewController())
            {
                if (GUILayout.Button(mLocaleText.Generate + " " + CodeGenUtil.GetBindBelongs2(mBindScript),
                        GUILayout.Height(30)))
                {
                    CreateViewControllerCode.DoCreateCodeFromScene(mBindScript.gameObject);
                }
            }
            else
            {
                DrawGenButton(rootGameObj,mLocaleText.Generate,mBindScript);
            }

            GUILayout.EndVertical();

            base.OnInspectorGUI();
        }

        protected virtual void DrawGenButton(GameObject rootGameObj,string generateText, AbstractBind bind)
        {

        }
    }
}