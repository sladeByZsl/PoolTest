using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.Common
{
    public class CustomScriptableObject : ScriptableObject
    {
#if UNITY_EDITOR
        public static bool SaveToAsset<T>(T instance, string saveFilePath) where T : ScriptableObject
        {
            if(instance == null || Application.isPlaying)
            {
                return false;
            }
            if(System.IO.File.Exists(saveFilePath) == false)
            {
                var dir = System.IO.Path.GetDirectoryName(saveFilePath);
                if(System.IO.Directory.Exists(dir) == false)
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                UnityEditor.AssetDatabase.CreateAsset(instance, saveFilePath);
            }
            UnityEditor.EditorUtility.SetDirty(instance);       // must set dirty for save
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
            return true;
        }
#endif
    }
}
