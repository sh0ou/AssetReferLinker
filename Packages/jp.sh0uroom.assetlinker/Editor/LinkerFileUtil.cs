using System;
using System.IO;
using Newtonsoft.Json;

namespace sh0uRoom.AssetLinker
{
    internal static class LinkerFileUtil
    {
        public static string GetLinkPath(string fileName)
        {
            return Path.Combine(LinkerConstants.FolderName, fileName + ".astlnk").Replace('\\', '/');
        }

        public static string[] GetAllLinkPaths()
        {
            if (!DirectoryExists(LinkerConstants.FolderName)) return Array.Empty<string>();
            return Directory.GetFiles(LinkerConstants.FolderName, "*.astlnk");
        }

        public static bool DirectoryExists(string path)
        {
            return !string.IsNullOrEmpty(path) && Directory.Exists(path);
        }

        public static bool FileExists(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        public static void EnsureFolder()
        {
            if (!DirectoryExists(LinkerConstants.FolderName))
            {
                Directory.CreateDirectory(LinkerConstants.FolderName);
            }
        }

        public static bool TryReadJson<T>(string path, out T data)
        {
            data = default(T);
            if (!FileExists(path)) return false;

            try
            {
                var json = File.ReadAllText(path);
                data = JsonConvert.DeserializeObject<T>(json);
                return data != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryWriteJson<T>(string path, T data, bool indent = true)
        {
            try
            {
                var json = indent ? JsonConvert.SerializeObject(data, Formatting.Indented)
                                  : JsonConvert.SerializeObject(data);
                File.WriteAllText(path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}