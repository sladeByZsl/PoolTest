﻿/****************************************************************************
 * Copyright (c) 2018.3 vin129
 * 
 * http://qframework.io
 * https://github.com/liangxiegame/QFramework
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 ****************************************************************************/

using UnityEngine;
using UnityEngine.UI;

namespace QFramework.Example
{
    public class UISectionPanelData : UIPanelData
    {
        // TODO: Query Mgr's Data
    }

    public partial class UISectionPanel : UIPanel
    {
        private const int SectionNumber = 4;

        private readonly Button[] mButtons = new Button[SectionNumber];

        protected override void OnInit(IUIData uiData = null)
        {
            for (var i = 0; i < mButtons.Length; i++)
            {
                var btn = Instantiate(SectionBtn);
                btn.transform.SetParent(SectionBtn.transform.parent);
                btn.transform.localScale = Vector3.one;
                btn.transform.localPosition = Vector3.zero;
                btn.name = "Section" + (i + 1);
                btn.gameObject.SetActive(true);

                btn.GetComponentInChildren<Text>().text = btn.name;
                var index = i;
                btn.onClick.AddListener(() => { ChoiceSection(index); });
                mButtons[i] = btn;
            }

            SettingBtn.onClick.AddListener(() =>
            {
                UIKit.OpenPanel<UISettingPanel>(UILevel.PopUI, prefabName: "Resources/UISettingPanel");
            });

            BackBtn.onClick.AddListener(() =>
            {
                CloseSelf();
                UIKit.OpenPanel<UIMenuPanel>(UILevel.Common, prefabName: "Resources/UIMenuPanel");
            });
        }

        protected override void ProcessMsg(int eventId, QMsg msg)
        {
            throw new System.NotImplementedException();
        }

        private void ChoiceSection(int i)
        {
            UIKit.HidePanel(name);
            UIKit.OpenPanel<UIGamePanel>(UILevel.Common, new UIGamePanelData { SectionNo = i + 1 },
                prefabName: "Resources/UIGamePanel");
        }

        protected override void OnClose()
        {
        }
    }
}