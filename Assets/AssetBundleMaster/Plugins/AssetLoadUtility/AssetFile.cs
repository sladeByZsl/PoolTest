using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleMaster.AssetLoad
{
    [System.Serializable]
    public class AssetFile
    {
        private string _name = string.Empty;
        public string FileName
        {
            get { return _name; }
            set
            {
                if(string.IsNullOrEmpty(_name))
                {
                    _name = value;
                }
            }
        }

        [SerializeField]
        public List<string> FinalFullNames = null;        // res with same name but not the same type res!!!
        
    }
}