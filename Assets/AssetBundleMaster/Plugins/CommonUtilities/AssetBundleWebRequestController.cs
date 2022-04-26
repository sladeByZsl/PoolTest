using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AssetBundleMaster.Common
{
    public class AssetBundleWebRequestController : Singleton<AssetBundleWebRequestController>
    {
        #region Defines
        private class AssetBundleDownloadTarget
        {
            public string url { get; private set; }
            public Hash128? hash { get; private set; }
            public AsyncOperation request { get; private set; }
            public UnityWebRequest unityWebRequest { get; private set; }
            public int maxFailedCount { get; private set; }

            public AssetBundle assetBundle { get; private set; }

            private System.Action<AssetBundle> _onloaded = null;
            public event System.Action<AssetBundle> onloaded
            {
                add
                {
                    _onloaded += value;
                    if(assetBundle)
                    {
                        Invoke(assetBundle);
                    }
                }
                remove { _onloaded -= value; }
            }

            private int _coroutineID = -1;

            public AssetBundleDownloadTarget(string url, Hash128? hash = null, int maxFailedCount = 0)
            {
                this.url = url;
                this.hash = hash;
                this.maxFailedCount = maxFailedCount;
            }

            public void SendWebRequest()
            {
                if(request == null)
                {
#if UNITY_2018_1_OR_NEWER
                    unityWebRequest = this.hash.HasValue ? UnityWebRequestAssetBundle.GetAssetBundle(url, hash.Value, 0) : UnityWebRequestAssetBundle.GetAssetBundle(url, 0);
                    request = UnityWebRequestSend(unityWebRequest);
                    request.completed += OnLoaded;
#elif UNITY_2017_1_OR_NEWER
                    unityWebRequest = this.hash.HasValue ? UnityWebRequest.GetAssetBundle(url, hash.Value, 0) : UnityWebRequest.GetAssetBundle(url);
                    request = UnityWebRequestSend(unityWebRequest);
                    request.completed += OnLoaded;
#else
                    if(this.hash.HasValue)
                    {
                        request = new AsyncOperation();
                        var www = WWW.LoadFromCacheOrDownload(url, hash.Value);
                        _coroutineID = CoroutineRoot.Instance.StartCoroutineWWW(www, OnLoaded);
                    }
                    else
                    {
#if UNITY_5_4_OR_NEWER
                        unityWebRequest = UnityWebRequest.GetAssetBundle(url);
                        request = UnityWebRequestSend(unityWebRequest);
                        _coroutineID = CoroutineRoot.Instance.StartCoroutineAsyncOperation(request, OnLoaded);
#else
                        request = new AsyncOperation();
                        var www = new WWW(url);
                        _coroutineID = CoroutineRoot.Instance.StartCoroutineWWW(www, OnLoaded);
#endif
                    }
#endif
                }
            }

            private void OnLoaded(AsyncOperation asyncOperation)
            {
                if(asyncOperation.isDone)
                {
                    bool isError = UnityWebRequestIsError(unityWebRequest);
                    if(false == isError)
                    {
                        this.assetBundle = DownloadHandlerAssetBundle.GetContent(unityWebRequest);
                    }
                    else
                    {
                        if(ToReload())
                        {
                            Abort();
                            SendWebRequest();
                            return;
                        }
                        else
                        {
                            Debug.LogErrorFormat("UnityWebRequest [{0}] Error : {1}", unityWebRequest.url, unityWebRequest.error);
                        }
                    }
                    Invoke(this.assetBundle);
                }
            }

#if !UNITY_2017_1_OR_NEWER
            private void OnLoaded(WWW www)
            {
                if (www.isDone)
                {
                    bool isError = (string.IsNullOrEmpty(www.error) == false);
                    if (false == isError)
                    {
                        this.assetBundle = www.assetBundle;
                    }
                    else
                    {
                        if (ToReload())
                        {
                            Abort();
                            SendWebRequest();
                            return;
                        }
                        else
                        {
                            Debug.LogErrorFormat("WWW [{0}] Error : {1}", www.url, www.error);
                        }
                    }
                    Invoke(this.assetBundle);
                }
            }
#endif

            private void Invoke(AssetBundle assetBundle)
            {
                if(_onloaded != null)
                {
                    var tempCall = _onloaded;
                    _onloaded = null;
                    tempCall.Invoke(assetBundle);
                }
            }

            public void Unload(bool unloadAllLoadedObjects)
            {
                request = null;
                if(unityWebRequest != null)
                {
                    using(unityWebRequest)
                    {
                        unityWebRequest.Abort();
                    }
                    unityWebRequest = null;
                }
                if(this.assetBundle)
                {
                    this.assetBundle.Unload(unloadAllLoadedObjects);
                    this.assetBundle = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log("AssetBundle.Unload(" + unloadAllLoadedObjects + ") : " + url);
#endif
                }
                _onloaded = null;
            }

            private bool ToReload()
            {
                if(maxFailedCount > 0)
                {
                    maxFailedCount--;
                    return true;
                }
                return false;
            }

            private void Abort()
            {
                CoroutineRoot.Instance.StopCoroutine(ref _coroutineID);
#if UNITY_2017_1_OR_NEWER
                if(request != null)
                {
                    request.completed -= OnLoaded;
                    request = null;
                }
#endif
            }
        }
        #endregion

        private static readonly Dictionary<string, AssetBundleDownloadTarget> ms_downloading = new Dictionary<string, AssetBundleDownloadTarget>();
        public int MaxFailedCount = 0;

        #region Main Funcs
        public AsyncOperation DownloadAssetBundle(string url, Hash128? hash, System.Action<AssetBundle> downloaded)
        {
            AssetBundleDownloadTarget target = null;
            if(ms_downloading.TryGetValue(url, out target) == false || target == null)
            {
                target = new AssetBundleDownloadTarget(url, hash, MaxFailedCount);
                ms_downloading[url] = target;
                target.SendWebRequest();
            }
            target.onloaded += downloaded;
            return target.request;
        }

        public void GetAssetBundleFileSize(string url, System.Action<long> result)
        {
            CoroutineRoot.Instance.StartCoroutineEx(GetFileSize(url, result));
        }

        public void Unload(string url, bool unloadAllLoadedObjects)
        {
            AssetBundleDownloadTarget target = null;
            if(ms_downloading.TryGetValue(url, out target) && target != null)
            {
                target.Unload(unloadAllLoadedObjects);
                ms_downloading.Remove(url);
            }
        }
        #endregion

        #region Help Funcs
        public static AsyncOperation UnityWebRequestSend(UnityWebRequest unityWebRequest)
        {
#if UNITY_2017_1_OR_NEWER
            return unityWebRequest.SendWebRequest();
#else
            return unityWebRequest.Send();
#endif
        }
        public static bool UnityWebRequestIsError(UnityWebRequest unityWebRequest)
        {
            bool isError = false;
#if UNITY_2020_1_OR_NEWER
            if (unityWebRequest.result == UnityWebRequest.Result.ConnectionError
                || unityWebRequest.result == UnityWebRequest.Result.DataProcessingError
                || unityWebRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                isError = true;
            }
#elif UNITY_2017_1_OR_NEWER
            if(unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
            {
                isError = true;

            }
#else
            if(unityWebRequest.isError)
            {
                isError = true;
            }
#endif
            return isError;
        }

        IEnumerator GetFileSize(string url, System.Action<long> resut)
        {
            UnityWebRequest unityWebRequest = UnityWebRequest.Head(url);
            yield return UnityWebRequestSend(unityWebRequest);
            if(UnityWebRequestIsError(unityWebRequest))
            {
                Debug.LogErrorFormat("Error While Getting Length [{0}] : {1}", url, unityWebRequest.error);
                if(resut != null)
                {
                    resut(-1);
                }
            }
            else
            {
                string size = unityWebRequest.GetResponseHeader("Content-Length");
                if(resut != null)
                {
                    resut(System.Convert.ToInt64(size));
                }
            }
        }
        #endregion

    }
}