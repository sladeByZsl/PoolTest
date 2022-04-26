using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssetBundleMaster.Example
{
    using AssetBundleMaster.ResourceLoad;

    public class Example_LoadAsset : MonoBehaviour
    {
        public Image Image1;
        public Image Image2;
        public RawImage RawImage;
        public Text Text;

        public Button LoadTextButton;
        public Button LoadTextureAsyncButton;
        public Button LoadSpritesButton;

        // Use this for initialization
        void Start()
        {
            LoadTextButton.onClick.AddListener(() =>
            {
                Debug.Log("LoadTextButton Start Load at frame : " + Time.frameCount);

                // Load Single Asset Sync
                var text = ResourceLoadManager.Instance.Load<TextAsset>("Sprites/Pic1");
                if(text)
                {
                    Text.text = text.text;
                    Debug.Log(Text.text + " Loaded at frame : " + Time.frameCount);
                }
            });

            LoadTextureAsyncButton.onClick.AddListener(() =>
            {
                Debug.Log("LoadTextureAsyncButton Start Load at frame : " + Time.frameCount);

                // Load Single Asset Async
                ResourceLoadManager.Instance.LoadAsync<Texture2D>("Textures/Pic3", (_tex) =>
                {
                    RawImage.texture = _tex;
                    Debug.Log(RawImage.texture + " Loaded at frame : " + Time.frameCount);
                });
            });

            LoadSpritesButton.onClick.AddListener(() =>
            {
                Debug.Log("LoadSpritesButton Start Load at frame : " + Time.frameCount);

                // load asset with ext name
                var sp1 = ResourceLoadManager.Instance.Load<Sprite>("Sprites/Pic1.tga");
                if(sp1)
                {
                    Image1.overrideSprite = sp1;
                    Debug.Log(Image1.overrideSprite + " Loaded at frame : " + Time.frameCount);
                }

                var sp2 = ResourceLoadManager.Instance.Load<Sprite>("Sprites/Pic1.png");
                if(sp2)
                {
                    Image2.overrideSprite = sp2;
                    Debug.Log(Image2.overrideSprite + " Loaded at frame : " + Time.frameCount);
                }
            });

            StartScene.RegisterClickFocus();
        }
    }
}

