using UnityEngine;
using System.Collections;
using UnityEditor;

namespace AssetBundleMaster.Editor
{
    public class MyEditorWindow<T> : EditorWindow where T : MyEditorWindow<T>
    {
        private static T _instance = null;

        public static void Init()
        {
            if (_instance == null)
            {
                _instance = GetWindow<T>(typeof(T).Name);
                _instance.position = new Rect(200, 200, 800, 600);
            }
            _instance.Show();
            _instance.Reload();
        }

        protected virtual void Reload() { }
    }
}

