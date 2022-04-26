using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace AssetBundleMaster.Editor
{
    using AssetBundleMaster.Extention;

#if UNITY_2019_1_OR_NEWER
    [CustomEditor(typeof(TextureImporter))]
    [CanEditMultipleObjects]
    public class TextureImporterCustomEditor : DecoratorEditor<TextureImporter>
    {
        private static readonly BindingFlags MethodFlag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod;

        private bool _changed = false;
        private MethodInfo _apply = null;
        private MethodInfo _resetValues = null;

        #region Mono Funcs
        public override void OnEnable()
        {
            base.OnEnable();

            _apply = _nativeEditor.GetType().GetMethod("Apply", MethodFlag);
            _resetValues = _nativeEditor.GetType().GetMethod("ResetValues", MethodFlag);
        }
        public override void OnInspectorGUI()
        {
            if(_target.textureType == TextureImporterType.Sprite)
            {
                _target.spritePackingTag = EditorGUILayout.TextField("Packing Tag", _target.spritePackingTag);
            }

            base.OnInspectorGUI();

            if(GUI.changed)
            {
                _changed = true;
            }

            if(_changed)
            {
                ChangedButtonGUI();
            }
        }
        private void OnDestroy()
        {
            if(_changed)
            {
                if(CommonEditorUtils.MessageBox("Apply Changes ?"))
                {
                    ApplyChanges();
                }
                else
                {
                    RevertChanges(true);
                }
            }
        }
        #endregion

        #region GUI
        protected void ChangedButtonGUI()
        {
            if(GUILayout.Button("Revert", GUILayout.MaxWidth(50.0f)))
            {
                RevertChanges();
            }
            if(GUILayout.Button("Apply", GUILayout.MaxWidth(50.0f)))
            {
                ApplyChanges();
            }
        }
        #endregion

        #region Main Funcs
        protected void ApplyChanges()
        {
            if(_changed)
            {
                _changed = false;
                EditorUtility.SetDirty(_target);
                if(_apply != null)
                {
                    _apply.Invoke(_nativeEditor, null);
                }
                _target.SaveAndReimport();
            }
        }
        protected void RevertChanges(bool unselectSelf = false)
        {
            if(_changed)
            {
                _changed = false;
                _resetValues.Invoke(_nativeEditor, null);
            }
            if (unselectSelf)
            {
                Selection.activeObject = null;
            }
        }
        #endregion
    }
#endif
}

