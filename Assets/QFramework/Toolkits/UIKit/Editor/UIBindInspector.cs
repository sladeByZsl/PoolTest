using UnityEditor;
using UnityEngine;

namespace QFramework
{
    [CustomEditor(typeof(AbstractBind), true)]
    public class UIBindInspector : AbstractBindInspector
    {
        protected override void DrawGenButton(GameObject rootGameObj,string generateText, AbstractBind bind)
        {
            if (rootGameObj.transform.IsUIPanel())
            {
                if (GUILayout.Button(generateText + " " + CodeGenUtil.GetBindBelongs2(bind),
                        GUILayout.Height(30)))
                {
                    var rootPrefabObj = PrefabUtility.GetCorrespondingObjectFromSource<Object>(rootGameObj);
                    UICodeGenerator.DoCreateCode(new[] { rootPrefabObj });
                }
            }
        }
        
        
    }

    public static class UIBindHelper
    {
        public static bool IsUIPanel(this Component component)
        {
            if (component.GetComponent<UIPanel>())
            {
                return true;
            }

            return false;
        }
    }
}