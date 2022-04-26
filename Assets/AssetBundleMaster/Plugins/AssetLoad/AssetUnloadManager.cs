using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.GameUtilities;
    using AssetBundleMaster.Common;

    public class AssetUnloadManager : SingletonComponent<AssetUnloadManager>
    {
        #region Defines
        public enum UnloadLogic
        {
            Unload,
            Release
        }

        public struct UnloadTarget
        {
            public UnloadLogic unloadLogic;
            public AssetBundleTarget assetBundleTarget;

            public void Unload()
            {
                if(assetBundleTarget != null && assetBundleTarget.referenceCount <= 0)
                {
                    switch(unloadLogic)
                    {
                        case UnloadLogic.Unload:
                            {
                                assetBundleTarget.Unload();
                            }
                            break;
                        case UnloadLogic.Release:
                            {
                                assetBundleTarget.Release(true);
                            }
                            break;
                    }
                }
            }
        }
        #endregion

        [Header("it can be less then 1, means every 1/maxUnloadPerFrame frames per unload")]
        [SerializeField]
        [Range(0.01f, 1000f)]
        public float maxUnloadPerFrame = 1f;
        private float _unloadCounter = 0f;

        public Containers.OrderedDictionary<int, UnloadTarget> unloadingAssetBundles = new Containers.OrderedDictionary<int, UnloadTarget>();

        #region Main Funcs
        public void EnqueueUnloadTarget(AssetBundleTarget target, UnloadLogic unloadLogic)
        {
            if(target != null)
            {
                var tag = new UnloadTarget()
                {
                    assetBundleTarget = target,
                    unloadLogic = unloadLogic
                };
                unloadingAssetBundles.Add(target.GetHashCode(), tag);
            }
        }

        private void Update()
        {
            int count = unloadingAssetBundles.Count;
            if(count > 0)
            {
                if(_unloadCounter < 1f)
                {
                    _unloadCounter += Mathf.Max(0.01f, maxUnloadPerFrame);
                }
                if(_unloadCounter >= 1f)
                {
                    for(int i = 0, imax = Mathf.Min(count, Mathf.FloorToInt(_unloadCounter)); i < imax; i++)
                    {
                        int index = (--count);
                        var target = unloadingAssetBundles[index];
                        unloadingAssetBundles.RemoveAt(index);
                        target.Unload();
                    }
                    _unloadCounter = 0f;
                }
            }
        }
        #endregion

    }
}