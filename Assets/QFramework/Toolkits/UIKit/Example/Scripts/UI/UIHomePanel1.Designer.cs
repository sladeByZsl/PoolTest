using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.Example
{
	// Generate Id:e800c289-6c32-4834-9a15-25b58ffff972
	public partial class UIHomePanel1
	{
		public const string Name = "UIHomePanel1";
		
		[SerializeField]
		public UnityEngine.RectTransform Child1;
		
		private UIHomePanel1Data mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Child1 = null;
			
			mData = null;
		}
		
		public UIHomePanel1Data Data
		{
			get
			{
				return mData;
			}
		}
		
		UIHomePanel1Data mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UIHomePanel1Data());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
