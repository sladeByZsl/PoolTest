/****************************************************************************
 * Copyright (c) 2017 ~ 2022.3 liangxiegame
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using UnityEngine;

namespace QFramework
{
    public abstract class AbstractBind : MonoBehaviour, IBind
    {
        public string Comment
        {
            get { return CustomComment; }
        }

        public Transform Transform
        {
            get { return transform; }
        }
        
        
        [HideInInspector] public string CustomComment;
        

        [HideInInspector] [SerializeField] private string mComponentName;

        public virtual string Name
        {
            get
            {
                if (string.IsNullOrEmpty(mComponentName))
                {
                    mComponentName = nameof(Transform);
                }

                return mComponentName;
            }
            set { mComponentName = value; }
        }
    }
}