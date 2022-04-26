using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssetBundleMaster.Example
{
    using AssetBundleMaster.ResourceLoad;

    public class Example_UnloadAssetEfficiency : MonoBehaviour
    {
        public Button LoadTextureButton;
        public Button UnloadTextureButton;

        public Button LoadCubeButton;
        public Button UnloadCubeButton;

        public RawImage textureImage;

        public const string TexLoadPath = "Textures/Pic3";
        public const string CubeLoadPath = "Prefabs/Cube";


        // Use this for initialization
        void Start()
        {
            LoadTextureButton.onClick.AddListener(() =>
            {
                ResourceLoadManager.Instance.LoadAsync<Texture2D>(TexLoadPath, (_tex) =>
                {
                    textureImage.texture = _tex;
                });
            });
            UnloadTextureButton.onClick.AddListener(() =>
            {
                ResourceLoadManager.Instance.UnloadAsset<Texture2D>(TexLoadPath);
                textureImage.texture = null;
            });

            LoadCubeButton.onClick.AddListener(() =>
            {
                for(int i = 0; i < 10; i++)
                {
                    PrefabLoadManager.Instance.SpawnAsync(CubeLoadPath, (_cube) =>
                    {
                        SetCubeInfo(_cube);
                    });
                }
            });
            UnloadCubeButton.onClick.AddListener(() =>
            {
                PrefabLoadManager.Instance.DestroyTargetInPool(CubeLoadPath);
            });

            StartScene.RegisterClickFocus();
        }

        private void SetCubeInfo(GameObject cube)
        {
            cube.transform.position = new Vector3(UnityEngine.Random.Range(-3f, 3f),
                UnityEngine.Random.Range(-2f, 4f),
                UnityEngine.Random.Range(-5f, 5f));
            cube.transform.rotation = UnityEngine.Random.rotation;
        }
    }
}