using UnityEngine;

namespace sh0uRoom.AssetLinker
{
    public class LinkerInfo
    {
        public readonly string[] assetStoreURLs = {
            "https://assetstore.unity.com/packages/"
        };
        public readonly string[] boothURLs = {
            "https://booth.pm/ja/items/",
            "https://booth.pm/en/items/",
            "https://booth.pm/ko/items/",
            "https://booth.pm/zh-tw/items/",
            "https://*.booth.pm/items/"
        };
        public readonly string[] gumroadURLs = {
            "https://*.gumroad.com/l/"
        };
        public readonly string[] githubURLs = {
            "https://github.com/"
        };
        public readonly string[] vketStoreURLs = {
            "https://store.vket.com/ja/items/",
            "https://store.vket.com/en/items/"
        };
    }

    [System.Serializable]
    public enum LicenseType
    {
        None = 0,
        AssetStoreEULA = 1, // Asset Store EULA
        MIT = 2,    // MIT License
        Apache = 3, // Apache License 2.0
        GPL = 4,    // General Public License
        LGPL = 5,   // Lesser GPL
        BSD = 6,    // BSD License
        CC = 7,     // Creative Commons
        VN3 = 8,    // VN3 License
        UVL = 9,    // Uni-Virtual License
        Other = 10
    }
}
