using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace sh0uRoom.AssetLinker
{
    public class LinkerViewer : EditorWindow
    {
        [SerializeField] private VisualTreeAsset linkerViewerUxml;
        [SerializeField] private VisualTreeAsset linkerViewerItemUxml;

        [MenuItem("Window/AssetLinker/Show Linker")]
        public static void CreateWindow()
        {
            var window = GetWindow<LinkerViewer>();
            window.titleContent = new GUIContent("LinkerViewer");
        }

        private void OnEnable()  => Refresh();
        private void OnDisable() => rootVisualElement.Clear();

        private void Refresh()
        {
            var rootUxml     = linkerViewerUxml.CloneTree();
            rootVisualElement.Add(rootUxml);
            var itemRootView = rootUxml.Q<ScrollView>();
            var missingInfo  = rootUxml.Q<Label>("MissingInfo");
            var loc          = Localizer.Instance;

            var missingCount = 0;

            if (LinkerFileUtil.DirectoryExists(LinkerConstants.FolderName))
            {
                var linkerPaths = LinkerFileUtil.GetAllLinkPaths();
                foreach (var path in linkerPaths)
                {
                    if (!LinkerFileUtil.TryReadJson(path, out LinkerData linker) || linker == null)
                        continue;
                    missingCount += BuildLinkerItemView(itemRootView, path, linker, loc);
                }
                UpdateMissingStatusLabel(missingInfo, missingCount, linkerPaths.Length, loc);
            }
            else
            {
                missingInfo.text        = loc.Translate("MISSINGINFO_ERROR");
                missingInfo.style.color = Color.yellow;
            }

            var dontShowField = rootUxml.Q<Toggle>("DontShow");
            dontShowField.label = loc.Translate("DONTSHOWAGAIN");
            dontShowField.value = !LinkerSettings.IsAutoShow;
            dontShowField.RegisterValueChangedCallback(evt =>
            {
                LinkerSettings.IsAutoShow = !evt.newValue;
            });
        }

        // ─────────────────────────────────────────────
        //  アイテムビュー構築
        // ─────────────────────────────────────────────

        /// <summary>
        /// リンカーアイテムの UI を構築して <paramref name="itemRootView"/> に追加する。
        /// 戻り値は欠損パス数。
        /// </summary>
        private int BuildLinkerItemView(ScrollView itemRootView, string path, LinkerData linker, Localizer loc)
        {
            var itemUxml = linkerViewerItemUxml.CloneTree();
            var itemView = itemUxml.Q<VisualElement>("ItemView");
            itemView.Q<Foldout>().text = linker.Name;

            var container   = itemView.contentContainer;
            container.Q<Label>("VendorInfo").text =
                $"{linker.Vendor} / {(linker.IsFree ? loc.Translate("FREE") : loc.Translate("PAID"))}";

            var actionView = container.Q<VisualElement>("ActionView");
            actionView.Q<Button>("Download").text = loc.Translate("DOWNLOAD_BUTTON");
            actionView.Q<Button>("License").text  = loc.Translate("LICENSE_BUTTON");
            actionView.Q<Button>("Where").text    = loc.Translate("WHERE_BUTTON");
            actionView.Q<Button>("Unlink").text   = loc.Translate("UNLINK_BUTTON");

            // ダウンロードボタン
            actionView.Q<Button>("Download").clicked += () =>
                OpenUrlWithConfirmation(linker.DownloadURL, loc);

            // ライセンスボタン
            var licenseButton = actionView.Q<Button>("License");
            licenseButton.SetEnabled(!string.IsNullOrEmpty(linker.LicenseURL));
            licenseButton.clicked += () => OpenUrlWithConfirmation(linker.LicenseURL, loc);

            // Where? ボタン
            actionView.Q<Button>("Where").clicked += () =>
            {
                var topPath = FindTopmostPath(linker.Paths);
                if (!string.IsNullOrEmpty(topPath))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(topPath);
                    if (obj != null)
                    {
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                        return;
                    }
                }
                EditorUtility.DisplayDialog("AssetLinker", loc.Translate("WHERE_NOT_FOUND"), "OK");
            };

            // リンク解除ボタン
            actionView.Q<Button>("Unlink").clicked += () =>
            {
                if (EditorUtility.DisplayDialog("Unlink", loc.Translate("UNLINK_MESSAGE"), "Yes", "No"))
                {
                    itemRootView.Remove(itemUxml);
                    System.IO.File.Delete(path);
                    LinkerProjectWindowDecorator.NotifyLinksChanged();
                }
            };

            // パス一覧 + 欠損チェック
            var missingCount   = 0;
            var isMissingFound = false;
            var pathsView      = container.Q<Foldout>("Paths").Q<ScrollView>();

            foreach (var assetPath in linker.Paths)
            {
                var exists    = LinkerFileUtil.FileExists(assetPath) || LinkerFileUtil.DirectoryExists(assetPath);
                var pathLabel = new Label(assetPath) { style = { color = exists ? Color.green : Color.red } };
                if (!exists) { missingCount++; isMissingFound = true; }
                pathsView.contentContainer.Add(pathLabel);
            }

            // 欠損有無で枠色を設定
            var borderColor = isMissingFound ? new Color(1f, 1f, 0f, 0.5f) : new Color(0f, 1f, 0f, 0.5f);
            var fold = itemView.Q<Foldout>();
            fold.style.borderTopColor    = borderColor;
            fold.style.borderBottomColor = borderColor;
            fold.style.borderLeftColor   = borderColor;
            fold.style.borderRightColor  = borderColor;

            itemRootView.Add(itemUxml);
            return missingCount;
        }

        // ─────────────────────────────────────────────
        //  静的ユーティリティ
        // ─────────────────────────────────────────────

        /// <summary>確認ダイアログを表示してから URL を開く。</summary>
        private static void OpenUrlWithConfirmation(string url, Localizer loc)
        {
            if (EditorUtility.DisplayDialog("Open URL", $"{loc.Translate("OPENURL_MESSAGE")}\n{url}", "Yes", "No"))
                Application.OpenURL(url);
        }

        /// <summary>パス配列の中でパス階層が最も浅いものを返す。</summary>
        private static string FindTopmostPath(string[] paths)
        {
            if (paths == null) return null;
            string topPath  = null;
            int    minDepth = int.MaxValue;
            foreach (var p in paths)
            {
                var norm = p?.Replace('\\', '/');
                if (string.IsNullOrEmpty(norm)) continue;
                var depth = norm.Split('/').Length;
                if (depth < minDepth) { minDepth = depth; topPath = norm; }
            }
            return topPath;
        }

        /// <summary>欠損状態に応じて MissingInfo ラベルを更新する。</summary>
        private static void UpdateMissingStatusLabel(Label label, int missingCount, int totalLinks, Localizer loc)
        {
            if (totalLinks == 0)
            {
                label.text        = loc.Translate("MISSINGINFO_ERROR");
                label.style.color = Color.yellow;
            }
            else if (missingCount > 0)
            {
                label.text        = $"{missingCount} {loc.Translate("MISSINGINFO_FOUND")}";
                label.style.color = Color.yellow;
            }
            else
            {
                label.text        = loc.Translate("MISSINGINFO_OK");
                label.style.color = Color.green;
            }
        }
    }
}

