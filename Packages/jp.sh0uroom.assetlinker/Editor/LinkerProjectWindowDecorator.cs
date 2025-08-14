using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace sh0uRoom.AssetLinker
{
    [InitializeOnLoad]
    public static class LinkerProjectWindowDecorator
    {
        // キャッシュ
        private static readonly HashSet<string> s_LinkedAssets = new();
        private static readonly HashSet<string> s_LinkedFolders = new();
        private static GUIStyle s_BadgeStyle;

        static LinkerProjectWindowDecorator()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            EditorApplication.projectChanged += RefreshCache;
            RefreshCache();
        }

        static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
                return;

            // フォルダ・ファイル両方を対象に判定
            if (IsLinkedAsset(assetPath))
            {
                EnsureStyles();

                // 右端にコンテンツサイズぴったりのバッジを描画
                const string mark = "∞";
                var content = new GUIContent(mark);
                var size = s_BadgeStyle.CalcSize(content);

                // 少しだけパディング
                const float padX = 2f;
                const float padY = 1f;
                float w = Mathf.Ceil(size.x) + padX * 2f;
                float h = Mathf.Min(selectionRect.height - 2f, Mathf.Ceil(size.y) + padY * 2f);

                // 下限クランプ（負や極小を防ぐ）
                w = Mathf.Max(8f, w);
                h = Mathf.Max(10f, h);

                // 右端に色付きラベルやアイコンを描画（リスト/グリッド双方で見える位置）
                bool isSmall = selectionRect.height <= 20f;
                var rect = isSmall
                    // リスト表示: 右端に縦中央
                    ? new Rect(selectionRect.xMax - w - 2f, selectionRect.y + (selectionRect.height - h) * 0.5f, w, h)
                    // グリッド表示: 右下に配置
                    : new Rect(selectionRect.xMax - w - 2f, selectionRect.yMax - h - 2f, w, h);

                EditorGUI.DrawRect(rect, new Color(0.3f, 0.8f, 0.3f, 0.5f)); // 緑色半透明
                EditorGUI.LabelField(rect, content, s_BadgeStyle);
            }
        }

        /// <summary>
        /// スタイルの初期化
        /// </summary>
        private static void EnsureStyles()
        {
            if (s_BadgeStyle != null) return;

            GUIStyle baseStyle = null;
            try { baseStyle = EditorStyles.boldLabel; }
            catch { baseStyle = null; }

            if (baseStyle == null) baseStyle = GUI.skin.label;

            s_BadgeStyle = new GUIStyle(baseStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                fontStyle = FontStyle.Bold
            };
        }

        /// <summary>
        /// キャッシュ更新
        /// </summary>
        private static void RefreshCache()
        {
            s_LinkedAssets.Clear();
            s_LinkedFolders.Clear();

            var linkerPaths = LinkerFileUtil.GetAllLinkPaths();
            foreach (var path in linkerPaths)
            {
                if (!LinkerFileUtil.TryReadJson(path, out LinkerData linker) || linker?.Paths == null)
                    continue;

                foreach (var p in linker.Paths)
                {
                    var n = NormalizePath(p);
                    if (string.IsNullOrEmpty(n) || n.EndsWith(".meta"))
                        continue;

                    if (AssetDatabase.IsValidFolder(n))
                        s_LinkedFolders.Add(n);
                    else
                        s_LinkedAssets.Add(n);
                }
            }
        }

        private static string NormalizePath(string p) => p?.Replace('\\', '/');

        static bool IsLinkedAsset(string assetPath)
        {
            assetPath = NormalizePath(assetPath);

            // 明示的に登録されたアセット
            if (s_LinkedAssets.Contains(assetPath))
                return true;

            // リンクされたフォルダ自身 or その配下のファイル/フォルダ
            foreach (var folder in s_LinkedFolders)
            {
                if (assetPath == folder) return true;
                if (assetPath.Length > folder.Length && assetPath.StartsWith(folder + "/"))
                    return true;
            }

            return false;
        }
    }
}