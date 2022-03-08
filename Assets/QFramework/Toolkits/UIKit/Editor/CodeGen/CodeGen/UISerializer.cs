using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace QFramework
{
    public class UISerializer
    {
	    public static void StartAddComponent2PrefabAfterCompile(GameObject uiPrefab)
	    {
		    var prefabPath = AssetDatabase.GetAssetPath(uiPrefab);
		    if (string.IsNullOrEmpty(prefabPath))
			    return;

		    var pathStr = EditorPrefs.GetString("AutoGenUIPrefabPath");
		    if (string.IsNullOrEmpty(pathStr))
		    {
			    pathStr = prefabPath;
		    }
		    else
		    {
			    pathStr += ";" + prefabPath;
		    }

		    EditorPrefs.SetString("AutoGenUIPrefabPath", pathStr);
	    }
	    
	    [DidReloadScripts]
	    private static void DoAddComponent2Prefab()
	    {
		    var pathStr = EditorPrefs.GetString("AutoGenUIPrefabPath");
		    if (string.IsNullOrEmpty(pathStr))
			    return;

		    EditorPrefs.DeleteKey("AutoGenUIPrefabPath");
		    Debug.Log(">>>>>>>SerializeUIPrefab: " + pathStr);

		    var assembly = GetAssemblyCSharp();

		    var paths = pathStr.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
		    var displayProgress = paths.Length > 3;
		    if (displayProgress) EditorUtility.DisplayProgressBar("", "Serialize UIPrefab...", 0);
			
		    for (var i = 0; i < paths.Length; i++)
		    {
			    var uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
			    SetObjectRef2Property(uiPrefab, uiPrefab.name, assembly);

			    // uibehaviour
			    if (displayProgress)
				    EditorUtility.DisplayProgressBar("", "Serialize UIPrefab..." + uiPrefab.name, (float) (i + 1) / paths.Length);
			    Debug.Log(">>>>>>>Success Serialize UIPrefab: " + uiPrefab.name);
		    }

		    AssetDatabase.SaveAssets();
		    AssetDatabase.Refresh();

		    if (displayProgress) EditorUtility.ClearProgressBar();
	    }
	    
	    public static void SetObjectRef2Property(GameObject obj, string scriptName, Assembly assembly,
			List<IBind> processedMarks = null)
		{
			
			if (null == processedMarks)
			{
				processedMarks = new List<IBind>();
			}

			var iBind = obj.GetComponent<IBind>();
			var className = string.Empty;

			if (iBind != null)
			{
				className = ViewControllerKit.Setting.Namespace + "." + iBind.Name;
			}
			else
			{
				className = ViewControllerKit.Setting.Namespace + "." + scriptName;
			}


			Debug.Log(className);
			
			var type = assembly.GetType(className);
			var component = obj.GetComponent(type) ?? obj.AddComponent(type);
			var sObj = new SerializedObject(component);
			
			var marks = obj.GetComponentsInChildren<IBind>(true);
			foreach (var elementMark in marks)
			{
				if (processedMarks.Contains(elementMark))
				{
					continue;
				}

				processedMarks.Add(elementMark);

				var propertyName = elementMark.Transform.name;
				sObj.FindProperty(propertyName).objectReferenceValue = elementMark.Transform.gameObject;
			}

			sObj.ApplyModifiedPropertiesWithoutUndo();
		}
	    public static Assembly GetAssemblyCSharp()
	    {
		    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
		    foreach (var a in assemblies)
		    {
			    if (a.FullName.StartsWith("Assembly-CSharp,"))
			    {
				    return a;
			    }
		    }

		    return null;
	    }
    }
    
    
}