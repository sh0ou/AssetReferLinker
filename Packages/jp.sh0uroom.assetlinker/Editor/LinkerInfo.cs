namespace sh0uRoom.AssetLinker
{
    public static class LinkerInfo
    {
        public static readonly string[] assetStoreURLs = {
            "https://assetstore.unity.com/packages/"
        };
        public static readonly string[] boothURLs = {
            "https://booth.pm/ja/items/",
            "https://booth.pm/en/items/",
            "https://booth.pm/ko/items/",
            "https://booth.pm/zh-tw/items/",
            "https://*.booth.pm/items/"
        };
        public static readonly string[] gumroadURLs = {
            "https://*.gumroad.com/l/"
        };
        public static readonly string[] githubURLs = {
            "https://github.com/"
        };
        public static readonly string[] vketStoreURLs = {
            "https://store.vket.com/ja/items/",
            "https://store.vket.com/en/items/"
        };
    }

    [System.Serializable]
    public enum Vendor
    {
        Unknown,
        AssetStore,
        Booth,
        Gumroad,
        GitHub,
        VKetStore
    }

    [System.Serializable]
    public class LinkerData
    {
        public string DownloadURL { get; set; }
        public string LicenseURL { get; set; }
        public Vendor Vendor { get; set; }
        public bool IsFree { get; set; }
        public string[] Paths { get; set; }
    }
}
