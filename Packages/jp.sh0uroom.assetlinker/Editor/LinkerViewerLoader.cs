using UnityEditor;
using UnityEngine;

namespace sh0uRoom.AssetLinker
{
    [InitializeOnLoad]
    public class LinkerViewerLoader
    {
        static LinkerViewerLoader() => EditorApplication.delayCall += ShowOnStartup;

        static void ShowOnStartup()
        {
            var isAlreadyShown = SessionState.GetBool(LinkerConstants.AlreadyShownSessionKey, false);
            if (!isAlreadyShown)
            {
                OnLinkerViewerLoader();
                SessionState.SetBool(LinkerConstants.AlreadyShownSessionKey, true);
            }
        }

        static void OnLinkerViewerLoader()
        {
            if (!LinkerFileUtil.DirectoryExists(LinkerConstants.FolderName)) return;

            Debug.Log("Validate Assets...");

            // missing があればウィンドウを開く
            var linkerPaths = LinkerFileUtil.GetAllLinkPaths();
            foreach (var path in linkerPaths)
            {
                if (!LinkerFileUtil.TryReadJson(path, out LinkerData linker) || linker == null)
                    continue;

                foreach (var p in linker.Paths ?? System.Array.Empty<string>())
                {
                    if (!LinkerFileUtil.FileExists(p) && !LinkerFileUtil.DirectoryExists(p))
                    {
                        LinkerViewer.CreateWindow();
                        return;
                    }
                }
            }
        }
    }
}
