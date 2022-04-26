using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.AssetLoad
{
    using AssetBundleMaster.Common;
    using AssetBundleMaster.ObjectPool;

    public sealed class AssetList : AssetLoaderBase
    {
        private List<AssetLoader> _assetList = null;
        private List<AssetLoader> m_assetList
        {
            get
            {
                if(_assetList == null)
                {
                    _assetList = GlobalAllocator<List<AssetLoader>>.Allocate();
                }
                return _assetList;
            }
        }

        private QueueCommands _queueCommands = null;
        public QueueCommands queueCommands
        {
            get
            {
                if(_queueCommands == null)
                {
                    _queueCommands = new QueueCommands();
                }
                return _queueCommands;
            }
        }

        public System.Type systemTypeInstance { get; private set; }
        public LoadThreadMode loadThreadMode
        {
            get
            {
                return loadAssetMode;
            }
            set
            {
                if(false == isDone && loadAssetMode != value && loadAssetMode == LoadThreadMode.Asynchronous)
                {
                    loadAssetMode = value;
                    ResetLoadingCommand();
                }
            }
        }

        private bool _loadAll = false;
        public bool loadAll
        {
            get
            {
                return _loadAll;
            }
            set
            {
                if(_loadAll != value && value)
                {
                    _loadAll = value;
                    ResetLoadingCommand();
                }
            }
        }
        private bool _allAssetLoading = false;

        private System.Func<Command, bool> _checkFunc = null;
        public System.Func<Command, bool> checkFunc
        {
            get
            {
                if(_checkFunc == null)
                {
                    _checkFunc = new System.Func<Command, bool>(CheckFunc);
                }
                return _checkFunc;
            }
        }

        public static System.Action<AssetList> onAssetListLoaded = null;
        public static System.Action<AssetList, UnityEngine.Object[]> assetChanged = null;
        private static readonly List<UnityEngine.Object> ms_tempLoadedAssets = new List<UnityEngine.Object>();
        private static readonly CommonAllocator<Command> ms_commandPool = null;

        static AssetList()
        {
            ms_commandPool = new CommonAllocator<Command>().Set(create: () =>
            {
                return new Command();
            },
            deAllocate: (_cmd) =>
            {
                _cmd.cmd = null;
                _cmd.finishFunc = null;
                _cmd.data = null;
            });

            GlobalAllocator<List<AssetLoader>>.Set(create: () => { return new List<AssetLoader>(); }, deAllocate: (_list) => { _list.Clear(); });
        }

        public AssetList(string loadPath, System.Type systemTypeInstance, LoadThreadMode loadMode, bool loadAllAssets)
            : base(loadPath, null, AssetSource.Resources, true)
        {
            this.loadAssetMode = loadMode;
            this._loadAll = loadAllAssets;
            this.systemTypeInstance = systemTypeInstance;
        }

        #region Main Funcs
        public override void LoadRequest(LoadThreadMode loadThreadMode, ThreadPriority priority = ThreadPriority.Normal)
        {
            this.loadThreadMode = loadThreadMode;
            if(isDone)
            {
                Call();
                return;
            }
            if(loadAll)
            {
                if(_assetList != null)
                {
                    foreach(var assetLoader in _assetList)
                    {
                        AssetLoaderLoad(assetLoader);
                        if(assetLoader.isDone == false && false == _allAssetLoading)
                        {
                            queueCommands.Enqueue(CreateWaitCommand(assetLoader), true);
                        }
                    }
                    _allAssetLoading = true;
                }
                AddOnLoadedCall();
            }
            else
            {
                // load first only
                LoadOneAssetByQueue(0);
            }
        }

        public override bool Unload()
        {
            bool anyUnload = false;
            if(_assetList != null)
            {
                foreach(var loader in _assetList)
                {
                    anyUnload |= loader.Unload();
                }
                GlobalAllocator<List<AssetLoader>>.DeAllocate(_assetList);
                _assetList = null;
            }
            Assets = null;
            unloaded = true;
            return anyUnload;
        }

        public bool AddLoader(AssetLoader loader)
        {
            if(m_assetList.Contains(loader) == false)
            {
                m_assetList.Add(loader);
                if(loader.assetBundleLoader != null)
                {
                    loader.assetBundleLoader.IncreaseReferenceCount(1);
                }
                return true;
            }
            return false;
        }

        public void ResetLoadingCommand()
        {
            isDone = false;
            if(_queueCommands != null)
            {
                _queueCommands.Clear((_cmdQueue) =>
                {
                    for(int i = 0, imax = _cmdQueue.Count; i < imax; i++)
                    {
                        ms_commandPool.DeAllocate(_cmdQueue.Dequeue());
                    }
                });
            }
        }

        public void AddOnLoadedCall()
        {
            if(_queueCommands != null && _queueCommands.isRunning)
            {
                var cmd = ms_commandPool.Allocate();
                cmd.finishFunc = OnAssetLoaded;
                _queueCommands.Enqueue(cmd, true);
            }
            else
            {
                Complete();
            }
        }

        public override void Call()
        {
            if(onAssetListLoaded != null)
            {
                onAssetListLoaded.Invoke(this);
            }
            base.Call();
        }
        #endregion

        #region Help Funcs
        private Command CreateWaitCommand(AssetLoader loader)
        {
            var cmd = ms_commandPool.Allocate();
            cmd.data = loader;
            cmd.finishFunc = checkFunc;
            return cmd;
        }
        private bool CheckFunc(Command cmd)
        {
            var _loader = cmd.data as AssetLoader;
            var loaded = _loader.isDone;
            if(loaded)
            {
                ms_commandPool.DeAllocate(cmd);
            }
            return loaded;
        }
        private bool OnAssetLoaded(Command cmd)
        {
            Complete();
            ms_commandPool.DeAllocate(cmd);
            return true;
        }
        private void Complete()
        {
            if(_assetList != null)
            {
                for(int i = 0; i < _assetList.Count; i++)
                {
                    var loader = _assetList[i];
                    if(loader != null && loader.Assets != null && loader.Assets.Length > 0)
                    {
                        foreach(var asset in loader.Assets)
                        {
                            if(asset)
                            {
                                ms_tempLoadedAssets.Add(asset);
                            }
                        }
                    }
                }
            }

            var oldAssets = Assets;
            if(ms_tempLoadedAssets.Count > 0)
            {
                if(Assets == null || Assets.Length != ms_tempLoadedAssets.Count)
                {
                    var assetArray = Utility.CreateAssetArray(systemTypeInstance, ms_tempLoadedAssets.Count);
                    ms_tempLoadedAssets.CopyTo(assetArray);
                    Assets = assetArray;
                }
                else
                {
                    ms_tempLoadedAssets.CopyTo(Assets);
                }
                ms_tempLoadedAssets.Clear();
            }
            isDone = true;
            if(assetChanged != null && oldAssets != Assets)
            {
                assetChanged.Invoke(this, oldAssets);
            }
            Call();
        }

        private void LoadOneAssetByQueue(int index)
        {
            if(index >= 0 && _assetList != null && index < _assetList.Count)
            {
                ResetLoadingCommand();
                var assetLoader = _assetList[index];
                AssetLoaderLoad(assetLoader);
                if(assetLoader.isDone == false)
                {
                    queueCommands.Enqueue(CreateWaitCommand(assetLoader), false);
                    OneAssetLoadedCmd(index);
                }
                else
                {
                    OneAssetLoadedCheck(index);
                }
            }
            else
            {
                Complete();
            }
        }
        private void OneAssetLoadedCheck(int index)
        {
            var assetLoader = _assetList[index];
            if(assetLoader.Assets != null && assetLoader.Assets.Length > 0)
            {
                Complete();
            }
            else
            {
                LoadOneAssetByQueue(index + 1);
            }
        }
        private void OneAssetLoadedCmd(int index)
        {
            var cmd = ms_commandPool.Allocate();
            cmd.data = new KeyValuePair<int, AssetList>(index, this);
            cmd.cmd = (_cmd) =>
            {
                var kv = (KeyValuePair<int, AssetList>)_cmd.data;
                kv.Value.OneAssetLoadedCheck(kv.Key);
                ms_commandPool.DeAllocate(cmd);
            };
            queueCommands.Enqueue(cmd, true);
        }

        private void AssetLoaderLoad(AssetLoader assetLoader)
        {
            assetLoader.loadAllAssets = loadAll;
            assetLoader.unloadable &= this.unloadable;
            assetLoader.LoadRequest(loadThreadMode);
        }
        #endregion

        #region Not Imp Func
        public override bool MoveNext()
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}

