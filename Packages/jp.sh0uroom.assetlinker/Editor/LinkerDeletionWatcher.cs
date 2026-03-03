using System.Collections.Generic;
using UnityEditor;

namespace sh0uRoom.AssetLinker
{
    /// <summary>
    /// AssetLinker で追跡中のフォルダが削除されようとした際に確認ダイアログを表示する。
    /// </summary>
    public class LinkerDeletionWatcher : AssetModificationProcessor
    {
        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            // パスを正規化（バックスラッシュ除去・末尾スラッシュ除去）
            var normalized = LinkerFileUtil.NormalizePath(assetPath);

            // フォルダのみ対象
            if (!AssetDatabase.IsValidFolder(normalized))
                return AssetDeleteResult.DidNotDelete;

            // このパスを Paths に含む .astlnk ファイルを収集
            var matchingFiles = new List<string>();
            var matchingNames = new List<string>();

            foreach (var linkerPath in LinkerFileUtil.GetAllLinkPaths())
            {
                if (!LinkerFileUtil.TryReadJson(linkerPath, out LinkerData linker) || linker?.Paths == null)
                    continue;

                foreach (var trackedPath in linker.Paths)
                {
                    if (LinkerFileUtil.NormalizePath(trackedPath) == normalized)
                    {
                        matchingFiles.Add(linkerPath);
                        matchingNames.Add(!string.IsNullOrEmpty(linker.Name) ? linker.Name : linker.FileName ?? linkerPath);
                        break;
                    }
                }
            }

            if (matchingFiles.Count == 0)
                return AssetDeleteResult.DidNotDelete;

            // ダイアログ表示
            var loc = Localizer.Instance;

            // 追跡しているアセット名を列挙
            var nameList = string.Join("\n ", matchingNames);
            var message = $"{loc.Translate("DELETE_TRACKED")}\n\n [{nameList}]";

            // 0: Unlink & Delete  /  1: Cancel  /  2: Delete only
            var choice = EditorUtility.DisplayDialogComplex(
                "AssetLinker",
                message,
                loc.Translate("UNLINK_AND_DELETE"),  // ok     (0)
                loc.Translate("CANCEL"),              // cancel (1)
                loc.Translate("DELETE_ONLY")          // alt    (2)
            );

            switch (choice)
            {
                case 0: // Unlink & Delete: .astlnk を削除してフォルダ削除を続行
                    foreach (var lp in matchingFiles)
                    {
                        System.IO.File.Delete(lp);
                    }
                    LinkerProjectWindowDecorator.NotifyLinksChanged();
                    return AssetDeleteResult.DidNotDelete;

                case 1: // Cancel: 削除をキャンセル
                    return AssetDeleteResult.FailedDelete;

                default: // 2 - Delete only: リンクはそのままフォルダのみ削除
                    return AssetDeleteResult.DidNotDelete;
            }
        }
    }
}
