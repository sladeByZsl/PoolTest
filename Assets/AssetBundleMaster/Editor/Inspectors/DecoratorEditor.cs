using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System;

namespace AssetBundleMaster.Editor
{
    /// <summary>
    /// decorate Unity's built-in inspector Editor.
    /// </summary>
    public class DecoratorEditor<T> : UnityEditor.Editor where T : UnityEngine.Object
    {
        protected T _target;
        protected UnityEditor.Editor _nativeEditor;
        private static Type _inspectorEditorType = null;

        public virtual void OnEnable()
        {
            _target = target as T;

            if(_inspectorEditorType == null)
            {
                foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    // get Inspector or Editor
                    var tagType = assembly.GetType("UnityEditor." + typeof(T).Name + "Inspector")
                        ?? assembly.GetType("UnityEditor." + typeof(T).Name + "Editor");
                    if(tagType != null)
                    {
                        _inspectorEditorType = tagType;
                        break;
                    }
                }
            }

            if(_inspectorEditorType != null)
            {
                _nativeEditor = UnityEditor.Editor.CreateEditor(serializedObject.targetObject, _inspectorEditorType);
            }
            else
            {
                _nativeEditor = UnityEditor.Editor.CreateEditor(serializedObject.targetObject);
            }
        }

        public override void OnInspectorGUI()
        {
            if(_nativeEditor)
            {
                _nativeEditor.OnInspectorGUI();
            }
        }
    }
}