using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace sh0uRoom.AssetLinker
{
    /// <summary>
    /// .unitypackage のインポート完了後に自動で LinkerCreator を開く。
    /// インポートされたアセットの共通ルートフォルダを自動的に選択状態にする。
    /// </summary>
    [InitializeOnLoad]
    public static class LinkerPackageImportWatcher
    {
        private static readonly List<string> s_ImportedPaths = new();
        private static bool s_IsPackageImporting = false;

        static LinkerPackageImportWatcher()
        {
            AssetDatabase.importPackageStarted += OnPackageImportStarted;
            AssetDatabase.importPackageCompleted += OnPackageImportCompleted;
            AssetDatabase.importPackageCancelled += OnPackageImportCancelled;
            AssetDatabase.importPackageFailed += OnPackageImportFailed;
        }

        private static void OnPackageImportStarted(string packageName)
        {
            s_IsPackageImporting = true;
            s_ImportedPaths.Clear();
        }

        /// <summary>
        /// LinkerAssetPostprocessor から呼ばれる。
        /// インポート中でなければ何もしない。
        /// </summary>
        internal static void TrackImportedAssets(string[] importedAssets)
        {
            if (!s_IsPackageImporting || importedAssets == null) return;
            foreach (var path in importedAssets)
            {
                if (!string.IsNullOrEmpty(path))
                    s_ImportedPaths.Add(path);
            }
        }

        private static void OnPackageImportCompleted(string packageName)
        {
            s_IsPackageImporting = false;

            // コピーしてからクリア
            var paths = s_ImportedPaths.ToList();
            s_ImportedPaths.Clear();

            if (paths.Count == 0) return;

            var rootFolder = FindCommonRoot(paths);
            if (string.IsNullOrEmpty(rootFolder)) return;

            // delayCall でインポート処理が完全に終わるのを待ってからウィンドウを開く
            EditorApplication.delayCall += () =>
            {
                // ルートフォルダを選択状態にしてから LinkerCreator を開く
                var folderObj = AssetDatabase.LoadAssetAtPath<Object>(rootFolder);
                if (folderObj != null)
                {
                    Selection.activeObject = folderObj;
                }

                LinkerCreator.CreateWindow();
            };
        }

        private static void OnPackageImportCancelled(string packageName)
        {
            s_IsPackageImporting = false;
            s_ImportedPaths.Clear();
        }

        private static void OnPackageImportFailed(string packageName, string errorMessage)
        {
            s_IsPackageImporting = false;
            s_ImportedPaths.Clear();
        }

        /// <summary>
        /// インポートされたパス群の共通ルートフォルダを返す。
        /// 例: ["Assets/Foo/A.cs", "Assets/Foo/Bar/B.cs"] → "Assets/Foo"
        /// </summary>
        private static string FindCommonRoot(List<string> paths)
        {
            if (paths == null || paths.Count == 0) return string.Empty;

            // 各パスの所属フォルダに変換し、重複除去・ソート
            var folders = paths
                .Select(p => AssetDatabase.IsValidFolder(p)
                    ? p
                    : Path.GetDirectoryName(p)?.Replace('\\', '/'))
                .Where(p => !string.IsNullOrEmpty(p) && p.StartsWith("Assets"))
                .Distinct()
                .OrderBy(p => p.Length)
                .ToList();

            if (folders.Count == 0) return string.Empty;
            if (folders.Count == 1) return folders[0];

            // 最短フォルダのセグメントを上から順に全フォルダの共通プレフィックスを探す
            var segments = folders[0].Split('/');
            for (int depth = segments.Length; depth >= 1; depth--)
            {
                var candidate = string.Join("/", segments.Take(depth));
                if (folders.All(f => f == candidate || f.StartsWith(candidate + "/"))
                    && AssetDatabase.IsValidFolder(candidate))
                {
                    return candidate;
                }
            }

            return "Assets";
        }
    }

    /// <summary>
    /// パッケージインポート中にのみ新規アセットのパスを収集する AssetPostprocessor。
    /// </summary>
    public class LinkerAssetPostprocessor : AssetPostprocessor
    {
        // 警告抑制のため static void にする（Unity の要件）
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            LinkerPackageImportWatcher.TrackImportedAssets(importedAssets);
        }
    }
}
