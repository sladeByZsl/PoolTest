/****************************************************************************
 * Copyright (c) 2022.3 liangxiegame UNDER MIT LICENSE
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace QFramework
{
    public static class BindCollector
    {
	    /// <summary>
		/// 
		/// </summary>
		/// <param name="rootTrans"></param>
		/// <param name="curTrans"></param>
		/// <param name="transFullName"></param>
		public static void SearchBinds(Transform curTrans, string transFullName,ViewControllerCodeInfo viewControllerCodeInfo = null, ElementCodeInfo parentElementCodeInfo = null,Type leafPanelType = null)
		{
			foreach (Transform childTrans in curTrans)
			{
				var uiMark = childTrans.GetComponent<IBind>();

				if (null != uiMark)
				{
					if (null == parentElementCodeInfo)
					{
						if (!viewControllerCodeInfo.BindInfos.Any(markedObjInfo => markedObjInfo.Name.Equals(uiMark.Transform.name)))
						{
							viewControllerCodeInfo.BindInfos.Add(new BindInfo
							{
								Name = uiMark.Transform.name,
								BindScript = uiMark,
								PathToRoot = PathToParent(childTrans, viewControllerCodeInfo.GameObjectName)
							});
							viewControllerCodeInfo.DicNameToFullName.Add(uiMark.Transform.name, transFullName + childTrans.name);
						}
						else
						{
							Debug.LogError("Repeat key: " + childTrans.name);
						}
					}
					else
					{
						if (!parentElementCodeInfo.BindInfos.Any(markedObjInfo => markedObjInfo.Name.Equals(uiMark.Transform.name)))
						{
							parentElementCodeInfo.BindInfos.Add(new BindInfo()
							{
								Name = uiMark.Transform.name,
								BindScript = uiMark,
								PathToRoot = PathToParent(childTrans, parentElementCodeInfo.BehaviourName)
							});
							parentElementCodeInfo.DicNameToFullName.Add(uiMark.Transform.name, transFullName + childTrans.name);
						}
						else
						{
							Debug.LogError("Repeat key: " + childTrans.name);
						}
					}
					
					{

						if (leafPanelType != null && uiMark.Transform.GetComponent(leafPanelType))
						{
							
						} else {
							SearchBinds(childTrans, transFullName + childTrans.name + "/", viewControllerCodeInfo,
								parentElementCodeInfo);
						}
					}
				}
				else
				{
					SearchBinds(childTrans, transFullName + childTrans.name + "/",viewControllerCodeInfo, parentElementCodeInfo);
				}
			}
		}
	    
	    static string PathToParent(Transform trans, string parentName)
	    {
		    var retValue = new StringBuilder(trans.name);

		    while (trans.parent != null)
		    {
			    if (trans.parent.name.Equals(parentName))
			    {
				    break;
			    }

			    retValue = trans.parent.name.Append("/").Append(retValue);

			    trans = trans.parent;
		    }

		    return retValue.ToString();
	    }
	    
	    /// <summary>
	    /// 添加前缀
	    /// </summary>
	    /// <param name="selfStr"></param>
	    /// <param name="toAppend"></param>
	    /// <returns></returns>
	    static StringBuilder Append(this string selfStr, string toAppend)
	    {
		    return new StringBuilder(selfStr).Append(toAppend);
	    }
    }
}