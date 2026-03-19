using UnityEngine;

namespace AHAKuo.Signalia.Framework.Packages
{
    [System.Serializable]
    public class PackageData
    {
        public string package;
        public string title;
        public string description;
        public string path;
        public string headerImage;
        public string assetStoreURL;
    }

    [System.Serializable]
    public class PackageList
    {
        public PackageData[] packages;
    }

    /// <summary>
    /// Provides read-only package metadata for the Signalia catalog window.
    /// </summary>
    public static class PackageInfos
    {
        private const string BlobsPath = "Assets/AHAKuo Creations/Signalia/Framework/PackageHandlers/packageinfos.info";

        public static PackageData[] Packages()
        {
            var json = System.IO.File.ReadAllText(BlobsPath);
            var data = JsonUtility.FromJson<PackageList>(json);
            return data.packages;
        }
    }
}