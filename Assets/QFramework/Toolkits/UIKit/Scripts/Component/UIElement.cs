/****************************************************************************
 * Copyright (c) 2017 ~ 2022.3 liangxie
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

namespace QFramework
{
    using UnityEngine;

    /// <summary>
    /// belone to a panel 
    /// </summary>
    public abstract class UIElement : QMonoBehaviour,IBind
    {
        public abstract string Name { get; }

        public string Comment
        {
            get { return string.Empty; }
        }

        public Transform Transform
        {
            get { return transform; }
        }

        // public virtual BindType BindType { get; } = BindType.Element;

        public override IManager Manager
        {
            get { return UIManager.Instance; }
        }
    }
}