/****************************************************************************
 * Copyright (c) 2017 xiaojun、imagicbell
 * Copyright (c) 2017 ~ 2022.3 liangxiegame UNDER MIT License 
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;

namespace QFramework
{
	using UnityEngine;
	using UnityEditor;
	using System.IO;

	public class UICodeGenerator
	{
		[MenuItem("Assets/@UI Kit - Create UICode (alt+c) &c")]
		public static void CreateUICode()
		{
			var objs = Selection.GetFiltered(typeof(GameObject), SelectionMode.Assets | SelectionMode.TopLevel);
			
			DoCreateCode(objs);
		}

		public static void DoCreateCode(Object[] objs)
		{
			mScriptKitInfo = null;

			var displayProgress = objs.Length > 1;
			if (displayProgress) EditorUtility.DisplayProgressBar("", "Create UIPrefab Code...", 0);
			for (var i = 0; i < objs.Length; i++)
			{
				mInstance.CreateCode(objs[i] as GameObject, AssetDatabase.GetAssetPath(objs[i]));
				if (displayProgress)
					EditorUtility.DisplayProgressBar("", "Create UIPrefab Code...", (float) (i + 1) / objs.Length);
			}

			AssetDatabase.Refresh();
			if (displayProgress) EditorUtility.ClearProgressBar();
		}
		

		private void CreateCode(GameObject obj, string uiPrefabPath)
		{
			if (obj != null)
			{
				var prefabType = PrefabUtility.GetPrefabType(obj);
				if (PrefabType.Prefab != prefabType)
				{
					return;
				}

				var clone = PrefabUtility.InstantiatePrefab(obj) as GameObject;
				if (null == clone)
				{
					return;
				}
				
				var panelCodeInfo = new ViewControllerCodeInfo();

				Debug.Log(clone.name);
				panelCodeInfo.GameObjectName = clone.name.Replace("(clone)", string.Empty);
				BindCollector.SearchBinds(clone.transform, "",panelCodeInfo);
				CreateUIPanelCode(obj, uiPrefabPath,panelCodeInfo);
				
				UISerializer.StartAddComponent2PrefabAfterCompile(obj);

				HotScriptBind(obj);
				
				Object.DestroyImmediate(clone);
			}
		}
        
		public static string GetLastDirName(string absOrAssetsPath)
		{
			var name = absOrAssetsPath.Replace("\\", "/");
			var dirs = name.Split('/');

			return dirs[dirs.Length - 2];
		}

		public static string GenSourceFilePathFromPrefabPath(string uiPrefabPath,string prefabName)
		{
			var strFilePath = String.Empty;
            
			var prefabDirPattern = UIKitSettingData.Load().UIPrefabDir;

			if (uiPrefabPath.Contains(prefabDirPattern))
			{
				strFilePath = uiPrefabPath.Replace(prefabDirPattern, UIKitSettingData.GetScriptsPath());

			}
			else if (uiPrefabPath.Contains("/Resources"))
			{
				strFilePath = uiPrefabPath.Replace("/Resources", UIKitSettingData.GetScriptsPath());
			}
			else
			{
				strFilePath = uiPrefabPath.Replace("/" + GetLastDirName(uiPrefabPath), UIKitSettingData.GetScriptsPath());
			}

			strFilePath.Replace(prefabName + ".prefab", string.Empty).CreateDirIfNotExistsUIKit();

			strFilePath = strFilePath.Replace(".prefab", ".cs");

			return strFilePath;
		}
		
		private void CreateUIPanelCode(GameObject uiPrefab, string uiPrefabPath,ViewControllerCodeInfo viewControllerCodeInfo)
		{
			if (null == uiPrefab)
				return;

			var behaviourName = uiPrefab.name;

			var strFilePath = GenSourceFilePathFromPrefabPath(uiPrefabPath, behaviourName);
			if(mScriptKitInfo != null){
				if (File.Exists(strFilePath) == false)
				{
					if(mScriptKitInfo.Templates != null && mScriptKitInfo.Templates[0] != null)
						mScriptKitInfo.Templates[0].Generate(strFilePath, behaviourName, ViewControllerKit.Setting.Namespace,null);
				}
			}
			else
			{
				if (File.Exists(strFilePath) == false)
				{
					UIPanelTemplate.Write(behaviourName,strFilePath,UIKitSettingData.Load().Namespace,UIKitSettingData.Load());
				}
			}

			CreateUIPanelDesignerCode(behaviourName, strFilePath,viewControllerCodeInfo);
			Debug.Log(">>>>>>>Success Create UIPrefab Code: " + behaviourName);
		}
		
		private void CreateUIPanelDesignerCode(string behaviourName, string uiUIPanelfilePath,ViewControllerCodeInfo viewControllerCodeInfo)
		{
			var dir = uiUIPanelfilePath.Replace(behaviourName + ".cs", "");
			var generateFilePath = dir + behaviourName + ".Designer.cs";
			if(mScriptKitInfo != null)
			{
				if(mScriptKitInfo.Templates != null && mScriptKitInfo.Templates[1] != null){
					mScriptKitInfo.Templates[1].Generate(generateFilePath, behaviourName, ViewControllerKit.Setting.Namespace, viewControllerCodeInfo);
				}
				mScriptKitInfo.HotScriptFilePath.CreateDirIfNotExistsUIKit();
				mScriptKitInfo.HotScriptFilePath = mScriptKitInfo.HotScriptFilePath + "/" + behaviourName + mScriptKitInfo.HotScriptSuffix;
				if (File.Exists(mScriptKitInfo.HotScriptFilePath) == false && mScriptKitInfo.Templates != null &&  mScriptKitInfo.Templates[2] != null){
					mScriptKitInfo.Templates[2].Generate(mScriptKitInfo.HotScriptFilePath, behaviourName, ViewControllerKit.Setting.Namespace, viewControllerCodeInfo);
				}
			}
			else
			{
				UIPanelDesignerTemplate.Write(behaviourName,dir,ViewControllerKit.Setting.Namespace,viewControllerCodeInfo,UIKitSettingData.Load());
			}

			foreach (var elementCodeData in viewControllerCodeInfo.ElementCodeDatas)
			{
				var elementDir = string.Empty;
				// elementDir = elementCodeData.BindInfo.BindScript.BindType == BindType.Element
					// ? (dir + behaviourName + "/").CreateDirIfNotExists()
					// : (Application.dataPath + "/" + UIKitSettingData.GetScriptsPath() + "/Components/").CreateDirIfNotExists();
				CreateUIElementCode(elementDir, elementCodeData);
			}
		}

		private static void CreateUIElementCode(string generateDirPath, ElementCodeInfo elementCodeInfo)
		{
			var panelFilePathWhithoutExt = generateDirPath + elementCodeInfo.BehaviourName;

			if (File.Exists(panelFilePathWhithoutExt + ".cs") == false)
			{
				UIElementCodeTemplate.Generate(panelFilePathWhithoutExt + ".cs",
					elementCodeInfo.BehaviourName, ViewControllerKit.Setting.Namespace, elementCodeInfo);
			}

			UIElementCodeComponentTemplate.Generate(panelFilePathWhithoutExt + ".Designer.cs",
				elementCodeInfo.BehaviourName, ViewControllerKit.Setting.Namespace, elementCodeInfo);

			foreach (var childElementCodeData in elementCodeInfo.ElementCodeDatas)
			{
				var elementDir = (panelFilePathWhithoutExt + "/").CreateDirIfNotExistsUIKit();
				CreateUIElementCode(elementDir, childElementCodeData);
			}
		}

		private static readonly UICodeGenerator mInstance = new UICodeGenerator();
		
		private static void HotScriptBind(GameObject uiPrefab){
			if(mScriptKitInfo != null && mScriptKitInfo.CodeBind != null)
			{
				mScriptKitInfo.CodeBind.Invoke(uiPrefab,mScriptKitInfo.HotScriptFilePath);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}		

		private static ScriptKitInfo mScriptKitInfo;
	}
}