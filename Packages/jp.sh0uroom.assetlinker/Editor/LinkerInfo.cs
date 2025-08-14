namespace sh0uRoom.AssetLinker
{
    public static class LinkerInfo
    {
        public static readonly string[] assetStoreURLs = {
            "https://assetstore.unity.com/packages/"
        };
        public static readonly string[] boothURLs = {
            "https://booth.pm/*/items/",
            "https://*.booth.pm/items/"
        };
        public static readonly string[] gumroadURLs = {
            "https://*.gumroad.com/l/"
        };
        public static readonly string[] githubURLs = {
            "https://github.com/"
        };
    }

    [System.Serializable]
    public enum Vendor
    {
        Unknown,
        AssetStore,
        Booth,
        Gumroad,
        GitHub
    }

    [System.Serializable]
    public class LinkerData
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public string DownloadURL { get; set; }
        public string LicenseURL { get; set; }
        public Vendor Vendor { get; set; }
        public bool IsFree { get; set; }
        public string[] Paths { get; set; }
    }
}
