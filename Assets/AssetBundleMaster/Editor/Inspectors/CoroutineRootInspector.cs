using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AssetBundleMaster.Editor
{
    using AssetBundleMaster.Common;
    using AssetBundleMaster.Extention;

    [CustomEditor(typeof(CoroutineRoot))]
    public class CoroutineRootInspector : UnityEditor.Editor
    {
        private CoroutineRoot _target = null;

        private Dictionary<int, CoroutineController> m_coroutineController;
        private ObjectPool.CommonAllocator<CoroutineController> _coroutineControllerPool;

        private void OnEnable()
        {
            _target = target as CoroutineRoot;
            m_coroutineController = CommonEditorUtils.GetFieldValue<Dictionary<int, CoroutineController>>(_target, "m_coroutineController");
            _coroutineControllerPool = CommonEditorUtils.GetFieldValue<ObjectPool.CommonAllocator<CoroutineController>>(_target, "_coroutineControllerPool");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Label("Running Coroutines...");
            if(m_coroutineController != null)
            {
                GUILayout.Label("Running Count : " + m_coroutineController.Count);
                foreach(var datas in m_coroutineController)
                {
                    var id = datas.Key;
                    var running = datas.Value;
                    GUILayout.Label("ID : " + id + "    running State : " + running.state);
                }
            }
            GUILayout.Space(10.0f);
            GUILayout.Label("freeList Coroutines...");
            if(_coroutineControllerPool != null)
            {
                var freeList = CommonEditorUtils.GetFieldValue<Queue<CoroutineController>>(_coroutineControllerPool, "_freeList");
                if(freeList != null)
                {
                    GUILayout.Label("freeList Count : " + freeList.Count);
                }
            }

            Repaint();
        }
    }
}