using System;
using UnityEngine;

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

    public enum Vendor
    {
        Unknown,
        AssetStore,
        Booth,
        Gumroad,
        GitHub,
        VKetStore
    }

    public class LinkerData
    {
        public string DownloadURL
        {
            get => DownloadURL;
            set
            {
                if (CheckURL(value))
                    LicenseURL = value;
            }
        }
        public string LicenseURL
        {
            get => LicenseURL;
            set
            {
                if (CheckURL(value))
                    LicenseURL = value;
            }
        }
        public Vendor Vendor { get; set; }
        public bool IsFree { get; set; }
        public string[] Paths { get; set; }

        public LinkerData(string downloadURL, string licenseURL, Vendor vendor, bool isFree, string[] paths)
        {
            DownloadURL = downloadURL;
            LicenseURL = licenseURL;
            Vendor = vendor;
            IsFree = isFree;
            Paths = paths;
        }

        public bool CheckURL(string value)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("https://"))
            {
                throw new ArgumentException("Invalid URL");
                return false;
            }
            return true;
        }
    }
}
