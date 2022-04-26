using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssetBundleMaster.Example
{
    using AssetBundleMaster.ResourceLoad;

    public class Example_UnloadAsset : MonoBehaviour
    {
        public Button LoadButton;
        public Button UnloadButton;

        public Image Image1;
        public Image Image1_2;
        public Image Image2;
        public RawImage RawImage;
        public Text Text;

        // Use this for initialization
        void Start()
        {
            LoadButton.onClick.AddListener(() =>
            {
                Debug.Log("Start Load at frame : " + Time.frameCount);
            
                ResourceLoadManager.Instance.LoadAllAsync<Sprite>("Sprites/Pic1", (_sprites) =>
                {
                    Image1.overrideSprite = _sprites[0];
                    Debug.Log(Image1.overrideSprite + " Loaded at frame : " + Time.frameCount);

                    Image1_2.overrideSprite = _sprites[1];
                    Debug.Log(Image1_2.overrideSprite + " Loaded at frame : " + Time.frameCount);

                });

                ResourceLoadManager.Instance.LoadAsync<TextAsset>("Sprites/Pic1", (_text) =>
                {
                    Text.text = _text.text;
                    Debug.Log(Text.text + " Loaded at frame : " + Time.frameCount);
                });
                
                ResourceLoadManager.Instance.LoadAsync<Sprite>("Sprites/Pic2.png", (_sprite) =>
                {
                    Image2.overrideSprite = _sprite;
                    Debug.Log(Image2.overrideSprite + " Loaded at frame : " + Time.frameCount);
                });

                // load like Resources.LoadAsync
                StartCoroutine(LoadAsset<Texture2D>("Textures/Pic3", (_tex2D) =>
                {
                    RawImage.texture = _tex2D;
                    Debug.Log(RawImage.texture + " Loaded at frame : " + Time.frameCount);
                }));
            });

            UnloadButton.onClick.AddListener(() =>
            {
                // Pic1 loaded Sprite and Text, unload type <Object> means all these types
                /* Notice : Here the TextAsset is also unloaded */
                ResourceLoadManager.Instance.UnloadAsset<Object>("Sprites/Pic1", true);    // ture means <T> is base type

                // Pic2 is loaded as Sprite only
                ResourceLoadManager.Instance.UnloadAsset<Sprite>("Sprites/Pic2.png", false);   // false means unload only type == <T>

                // Pic3 is loaded as Texture only, we can call unload like Resources.UnloadAsset
                ResourceLoadManager.Instance.UnloadAsset(RawImage.texture);
                
                // unload request is tick later, so you can clear reference after call UnloadAsset
                // Notick : unload request is not a force unload you should set reference to null
                Image1.overrideSprite = null;
                Image1_2.overrideSprite = null;
                Image2.overrideSprite = null;
                RawImage.texture = null;
            });

            StartScene.RegisterClickFocus();

        }

        IEnumerator LoadAsset<T>(string loadPath, System.Action<T> loaded) where T : UnityEngine.Object
        {
            var loading = ResourceLoadManager.Instance.LoadAsync<T>(loadPath);
            yield return loading;
            loaded.Invoke(loading.asset as T);
        }
    }
}