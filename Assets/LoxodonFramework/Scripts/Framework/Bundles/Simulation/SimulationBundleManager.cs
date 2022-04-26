﻿#if UNITY_EDITOR
using System;
using System.Collections.Generic;

using Loxodon.Log;
using Loxodon.Framework.Asynchronous;

namespace Loxodon.Framework.Bundles
{
    public class SimulationBundleManager : IBundleManager
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<string, SimulationBundle> bundles;
        private List<string> bundleNames;

        public SimulationBundleManager()
        {
            this.bundles = new Dictionary<string, SimulationBundle>();
            this.bundleNames = AssetDatabaseHelper.GetUsedAssetBundleNames();
        }

        public virtual void AddBundle(SimulationBundle bundle)
        {
            if (this.bundles == null)
                return;

            this.bundles.Add(bundle.Name, bundle);
        }

        public virtual void RemoveBundle(SimulationBundle bundle)
        {
            if (this.bundles == null)
                return;

            this.bundles.Remove(bundle.Name);
        }

        protected virtual SimulationBundle GetOrCreateBundle(string bundleName)
        {
            var bundleNameWhitoutExtension = Path.GetFilePathWithoutExtension(bundleName).ToLower();
            var extension = Path.GetExtension(bundleName).ToLower();
            var bundleNameWhitExtension = string.IsNullOrEmpty(extension) ? bundleNameWhitoutExtension : string.Format("{0}.{1}", bundleNameWhitoutExtension, extension);

            if (bundleNames.IndexOf(bundleNameWhitExtension) < 0)
                throw new Exception(string.Format("Not found the AssetBundle '{0}'.", bundleNameWhitExtension));

            SimulationBundle bundle;
            if (this.bundles.TryGetValue(bundleNameWhitoutExtension, out bundle))
                return bundle;

            return new SimulationBundle(bundleNameWhitoutExtension, extension, this);
        }

        public virtual IBundle GetBundle(string bundleName)
        {
            if (this.bundles == null)
                return null;

            SimulationBundle bundle;
            if (this.bundles.TryGetValue(bundleName, out bundle))
                return new SimulationInternalBundleWrapper(bundle);
            return null;
        }

        public virtual IProgressResult<float, IBundle> LoadBundle(string bundleName)
        {
            return this.LoadBundle(bundleName, 0);
        }

        public virtual IProgressResult<float, IBundle> LoadBundle(string bundleName, int priority)
        {
            try
            {
                if (string.IsNullOrEmpty(bundleName))
                    throw new ArgumentNullException("bundleName", "The bundleName is null or empty!");

                SimulationBundle bundle = this.GetOrCreateBundle(bundleName);
                return new ImmutableProgressResult<float, IBundle>(new SimulationInternalBundleWrapper(bundle), 1f);
            }
            catch (Exception e)
            {
                return new ImmutableProgressResult<float, IBundle>(e, 0f);
            }
        }
        public virtual IProgressResult<float, IBundle[]> LoadBundle(params string[] bundleNames)
        {
            return this.LoadBundle(bundleNames, 0);
        }

        public virtual IProgressResult<float, IBundle[]> LoadBundle(string[] bundleNames, int priority)
        {
            try
            {
                if (bundleNames == null || bundleNames.Length <= 0)
                    throw new ArgumentNullException("bundleNames", "The bundleNames is null or empty!");

                List<IBundle> list = new List<IBundle>(0);
                foreach (string bundleName in bundleNames)
                {
                    try
                    {
                        list.Add(new SimulationInternalBundleWrapper(this.GetOrCreateBundle(bundleName)));
                    }
                    catch (Exception e)
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat("Loads Bundle '{0}' failure! Error:{1}", bundleName, e);
                    }
                }
                return new ImmutableProgressResult<float, IBundle[]>(list.ToArray(), 1f);
            }
            catch (Exception e)
            {
                return new ImmutableProgressResult<float, IBundle[]>(e, 0f);
            }
        }
    }
}
#endif