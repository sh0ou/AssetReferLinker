using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace sh0uRoom.AssetLinker
{
    public class LinkerCreator : EditorWindow
    {
        [SerializeField] private VisualTreeAsset linkerCreatorUxml;
        [SerializeField] private VisualTreeAsset linkerCreatorItemUxml;
        private const string UI_TEXTBG_NAME = "unity-text-input";

        private Dictionary<string, bool> _toggleStates;
        private Dictionary<string, Toggle> _toggleByPath;
        private Dictionary<string, List<string>> _childrenByFolder;
        private Dictionary<string, string> _parentByPath;

        [MenuItem("Assets/AssetLinker")]
        public static void CreateWindow()
        {
            var window = GetWindow<LinkerCreator>();
            window.titleContent = new GUIContent("LinkerCreator");
        }

        private void OnEnable()
        {
            //ルート要素の作成
            var asset = linkerCreatorUxml.CloneTree();
            rootVisualElement.Add(asset);
            var rootView = asset.Q<VisualElement>("View");
            var selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            var loc = Localizer.Instance;

            var fileNameField = rootView.Q<TextField>("FileName");
            fileNameField.label = loc.Translate("FILE_NAME");
            fileNameField.Q<VisualElement>("unity-text-input").style.opacity = 0.5f;

            var fileName = string.Empty;
            if (string.IsNullOrEmpty(selectionPath))
            {
                // 未選択ならフォルダダイアログ
                var chosen = EditorUtility.OpenFolderPanel("Select Folder", "", "");
                if (!string.IsNullOrEmpty(chosen))
                {
                    var unityPath = ToUnityAssetPath(chosen);
                    if (!string.IsNullOrEmpty(unityPath))
                    {
                        fileName = Path.GetFileName(unityPath);
                        selectionPath = unityPath.Replace('\\', '/');
                    }
                }
            }
            else
            {
                // 最下層の名前を取得
                fileName = Path.GetFileName(selectionPath);
            }
            fileNameField.value = fileName;

            var fileNameHelpbox = rootView.Q<HelpBox>("FileNameWarning");
            fileNameHelpbox.text = loc.Translate("FILE_NAME_WARNING");

            var downloadURLField = rootView.Q<TextField>("DownloadURL");
            downloadURLField.label = loc.Translate("DOWNLOAD_URL");
            if (string.IsNullOrEmpty(selectionPath))
            {
                downloadURLField.style.display = DisplayStyle.None;
                return;
            }
            else
            {
                downloadURLField.style.display = DisplayStyle.Flex;
            }

            var filePathField = rootView.Q<TextField>("FilePath");
            filePathField.label = loc.Translate("ASSET_PATH");
            filePathField.Q<VisualElement>("unity-text-input").style.opacity = 0.5f;
            filePathField.value = selectionPath;

            var detailView = rootView.Q<VisualElement>("DetailView");

            //ダウンロードURL
            var vendorInfo = detailView.Q<Label>("VendorInfo");
            var vendor = Vendor.Unknown;
            downloadURLField.RegisterValueChangedCallback((evt) =>
            {
                var back = downloadURLField.Q<VisualElement>(UI_TEXTBG_NAME);
                if (!string.IsNullOrEmpty(downloadURLField.value) && downloadURLField.value.StartsWith("https://"))
                {
                    detailView.style.display = DisplayStyle.Flex;
                    vendor = UpdateVendorInfo(downloadURLField, vendorInfo);
                    ColorUtility.TryParseHtmlString("#2A2A2A", out Color color);
                    back.style.backgroundColor = color;
                }
                else
                {
                    detailView.style.display = DisplayStyle.None;
                    ColorUtility.TryParseHtmlString("#320000", out Color color);
                    back.style.backgroundColor = color;
                }
            });

            //アセット名
            var assetNameField = detailView.Q<TextField>("AssetName");
            assetNameField.label = loc.Translate("ASSET_NAME");

            //ライセンスURL
            var licenseURLField = detailView.Q<TextField>("LicenseURL");
            licenseURLField.label = loc.Translate("LICENSE_URL");
            var licenseURLWarning = detailView.Q<HelpBox>("LicenseURLWarning");
            licenseURLWarning.text = loc.Translate("LICENSE_WARNING");
            licenseURLField.RegisterValueChangedCallback((evt) =>
            {
                if (string.IsNullOrEmpty(licenseURLField.value) || !licenseURLField.value.StartsWith("https://"))
                {
                    licenseURLWarning.style.display = DisplayStyle.Flex;
                }
                else
                {
                    licenseURLWarning.style.display = DisplayStyle.None;
                }
            });

            var isFreeToggle = detailView.Q<Toggle>("IsFree");
            isFreeToggle.label = loc.Translate("ISFREE");

            // ファイルパス（親フォルダも含め、フォルダ⇔子で同期＆親は三状態表示）
            var filePathsFoldout = detailView.Q<Foldout>("FilePaths");
            filePathsFoldout.text = loc.Translate("FILEPATHS");
            var listContainer = filePathsFoldout.Q<ScrollView>().contentContainer;

            // 状態テーブル初期化
            _toggleStates = new Dictionary<string, bool>(256);
            _toggleByPath = new Dictionary<string, Toggle>(256);
            _childrenByFolder = new Dictionary<string, List<string>>(128);
            _parentByPath = new Dictionary<string, string>(512);

            // 収集
            var allFolderPaths = new List<string>();
            var allFilePaths = new List<string>();

            if (AssetDatabase.IsValidFolder(selectionPath))
            {
                // 親フォルダもトラッキング
                allFolderPaths.Add(selectionPath);
                CollectSubFoldersRecursive(selectionPath, allFolderPaths);

                // 配下ファイル
                var guidsInRoot = AssetDatabase.FindAssets("", new[] { selectionPath });
                for (int i = 0; i < guidsInRoot.Length; i++)
                {
                    var p = AssetDatabase.GUIDToAssetPath(guidsInRoot[i]);
                    if (string.IsNullOrEmpty(p) || AssetDatabase.IsValidFolder(p)) continue;
                    allFilePaths.Add(p);
                }
            }
            else
            {
                // 単体 or 混在
                var guids = AssetDatabase.FindAssets("", new[] { selectionPath });
                if (guids != null && guids.Length > 0)
                {
                    for (int i = 0; i < guids.Length; i++)
                    {
                        var p = AssetDatabase.GUIDToAssetPath(guids[i]);
                        if (string.IsNullOrEmpty(p)) continue;
                        if (AssetDatabase.IsValidFolder(p)) allFolderPaths.Add(p);
                        else allFilePaths.Add(p);
                    }
                }
                else if (!string.IsNullOrEmpty(selectionPath))
                {
                    if (AssetDatabase.IsValidFolder(selectionPath)) allFolderPaths.Add(selectionPath);
                    else allFilePaths.Add(selectionPath);
                }
            }

            // ソート: フォルダ→ファイル
            allFolderPaths.Sort(System.StringComparer.Ordinal);
            allFilePaths.Sort(System.StringComparer.Ordinal);

            // 親子関係マップ
            var allPaths = new List<string>(allFolderPaths.Count + allFilePaths.Count);
            allPaths.AddRange(allFolderPaths);
            allPaths.AddRange(allFilePaths);

            foreach (var p in allPaths)
            {
                var parent = GetParentPath(p);
                if (!string.IsNullOrEmpty(parent) && parent.StartsWith(selectionPath))
                {
                    _parentByPath[p] = parent;
                    if (!_childrenByFolder.TryGetValue(parent, out var list))
                    {
                        list = new List<string>();
                        _childrenByFolder[parent] = list;
                    }
                    list.Add(p);
                }
            }

            // UI 生成
            foreach (var p in allPaths)
            {
                var item = linkerCreatorItemUxml.CloneTree();
                var toggle = item.Q<VisualElement>("ItemView").Q<Toggle>("IsLink");

                // 初期は全て ON、三状態は OFF
                toggle.SetValueWithoutNotify(true);
                toggle.showMixedValue = false;
                _toggleStates[p] = true;
                _toggleByPath[p] = toggle;

                toggle.Q<Label>("Path").text = p;

                toggle.RegisterValueChangedCallback(evt =>
                {
                    // 明示操作なら三状態を解除し、値を採用
                    toggle.showMixedValue = false;
                    _toggleStates[p] = evt.newValue;

                    // フォルダなら子孫へ同期
                    if (AssetDatabase.IsValidFolder(p))
                        SetChildrenStateRecursive(p, evt.newValue);

                    // 親の三状態を更新
                    UpdateAncestorsState(p, selectionPath);
                });

                listContainer.Add(item);
            }

            //警告文
            var modifyWarning = detailView.Q<HelpBox>("ModifyWarning");
            modifyWarning.text = $"<b><size=14>{loc.Translate("CREATE_WARNING_0")}</size></b>\n{loc.Translate("CREATE_WARNING_1")}";

            //リンク作成
            var createButton = rootView.Q<Button>("LinkButton");
            createButton.RegisterCallback<ClickEvent>((evt) =>
            {
                var linkerData = new LinkerData
                {
                    Name = assetNameField.value,
                    FileName = fileNameField.value,
                    DownloadURL = downloadURLField.value,
                    LicenseURL = licenseURLField.value,
                    Vendor = vendor,
                    IsFree = isFreeToggle.value,
                    Paths = _toggleStates.Where(x => x.Value).Select(x => x.Key).ToArray()
                };

                CreateLink(linkerData);

                if (IsLinked(linkerData.FileName))
                {
                    var message = loc.Translate("CREATE_OK");
                    EditorUtility.DisplayDialog("LinkerCreator", message, "OK");
                    Close();
                }
                else
                {
                    var message = loc.Translate("CREATE_NG");
                    EditorUtility.DisplayDialog("LinkerCreator", message, "OK");
                }
            });

            //リンク情報
            var linkInfo = rootView.Q<Label>("LinkInfo");
            if (IsLinked(fileNameField.value))
            {
                linkInfo.text = "Linked!";
                linkInfo.style.color = Color.green;
            }
            else
            {
                linkInfo.text = "Not Linked";
                linkInfo.style.color = Color.yellow;
            }
        }

        private void OnDisable()
        {
            rootVisualElement.Clear();
        }

        // private static bool ValidateFile(string name)
        // {
        //     var path = $"AssetLinker/{name}.astlnk";
        //     return System.IO.File.Exists(path) && System.IO.File.ReadAllText(path).Contains("DownloadURL");
        // }

        private static bool IsLinked(string fileName)
        {
            var path = LinkerFileUtil.GetLinkPath(fileName);
            if (!LinkerFileUtil.FileExists(path)) return false;

            try
            {
                var json = File.ReadAllText(path);
                return !string.IsNullOrEmpty(json) && json.Contains("DownloadURL");
            }
            catch
            {
                return false;
            }
        }

        private void CreateLink(LinkerData data)
        {
            try
            {
                LinkerFileUtil.EnsureFolder();
                var path = LinkerFileUtil.GetLinkPath(data.FileName);
                if (!LinkerFileUtil.TryWriteJson(path, data, indent: true))
                {
                    throw new System.Exception("Failed to write json.");
                }

                // リンク作成を通知してProjectウィンドウを更新
                LinkerProjectWindowDecorator.NotifyLinksChanged();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        // OSパス→Unityアセットパス(Assets/...)に変換
        private static string ToUnityAssetPath(string systemPath)
        {
            if (string.IsNullOrEmpty(systemPath)) return null;
            systemPath = systemPath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');
            if (systemPath.StartsWith(dataPath))
                return "Assets" + systemPath.Substring(dataPath.Length);
            return null;
        }

        // 親パスを取得（なければ null）
        private static string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var i = path.LastIndexOf('/');
            if (i <= 0) return null;
            return path.Substring(0, i);
        }

        // サブフォルダを再帰収集
        private static void CollectSubFoldersRecursive(string root, List<string> result)
        {
            var queue = new Queue<string>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                var subs = AssetDatabase.GetSubFolders(cur);
                if (subs == null || subs.Length == 0) continue;
                foreach (var s in subs)
                {
                    result.Add(s);
                    queue.Enqueue(s);
                }
            }
        }

        // 子孫（フォルダ/ファイル）を再帰的に同じ値に
        private void SetChildrenStateRecursive(string folderPath, bool value)
        {
            if (!_childrenByFolder.TryGetValue(folderPath, out var children)) return;
            for (int i = 0; i < children.Count; i++)
            {
                var c = children[i];
                if (_toggleByPath.TryGetValue(c, out var t))
                {
                    t.showMixedValue = false; // 直接操作時は混在を解除
                    t.SetValueWithoutNotify(value);
                }
                _toggleStates[c] = value;

                if (AssetDatabase.IsValidFolder(c))
                    SetChildrenStateRecursive(c, value);
            }
        }

        // 親の状態（三状態）を上へ反映
        private void UpdateAncestorsState(string path, string root)
        {
            var parent = GetParentPath(path);
            while (!string.IsNullOrEmpty(parent) && parent.StartsWith(root))
            {
                bool anyOn = false;
                bool anyOff = false;

                if (_childrenByFolder.TryGetValue(parent, out var children))
                {
                    for (int i = 0; i < children.Count; i++)
                    {
                        var child = children[i];
                        bool v = _toggleStates.TryGetValue(child, out var b) && b;
                        anyOn |= v;
                        anyOff |= !v;
                        if (anyOn && anyOff) break;
                    }
                }

                // 三状態の決定
                if (_toggleByPath.TryGetValue(parent, out var pt))
                {
                    if (anyOn && anyOff)
                    {
                        pt.showMixedValue = true;    // 一部選択表示
                        pt.SetValueWithoutNotify(false);
                        _toggleStates[parent] = false;
                    }
                    else
                    {
                        pt.showMixedValue = false;
                        bool on = anyOn && !anyOff;  // 全ON
                        pt.SetValueWithoutNotify(on);
                        _toggleStates[parent] = on;
                    }
                }

                parent = GetParentPath(parent);
            }
        }

        private static Vendor UpdateVendorInfo(TextField downloadURLField, Label labelField)
        {
            if (string.IsNullOrEmpty(downloadURLField.value) || !downloadURLField.value.StartsWith("https://"))
            {
                labelField.text = "";
                labelField.style.display = DisplayStyle.None;
                return Vendor.Unknown;
            }

            labelField.style.display = DisplayStyle.Flex;

            if (TryUpdateVendorLabel(downloadURLField.value, LinkerInfo.assetStoreURLs, labelField, Vendor.AssetStore))
                return Vendor.AssetStore;

            if (TryUpdateVendorLabel(downloadURLField.value, LinkerInfo.boothURLs, labelField, Vendor.Booth))
                return Vendor.Booth;

            if (TryUpdateVendorLabel(downloadURLField.value, LinkerInfo.gumroadURLs, labelField, Vendor.Gumroad))
                return Vendor.Gumroad;

            if (TryUpdateVendorLabel(downloadURLField.value, LinkerInfo.githubURLs, labelField, Vendor.GitHub))
                return Vendor.GitHub;

            labelField.text = Localizer.Instance.Translate("VENDOR_UNKNOWN");
            labelField.style.color = Color.yellow;
            return Vendor.Unknown;
        }

        private static bool TryUpdateVendorLabel(string url, IEnumerable<string> targetUrls, Label labelField, Vendor vendor)
        {
            foreach (var targetUrl in targetUrls)
            {
                var pattern = "^" + Regex.Escape(targetUrl).Replace("\\*", ".*") + ".*$";
                if (Regex.IsMatch(url, pattern))
                {
                    var message = Localizer.Instance.Translate("VENDOR_INFO");
                    labelField.text = vendor.ToString() + message;
                    labelField.style.color = Color.green;
                    return true;
                }
            }
            return false;
        }

        private bool ValidateAssetName(string name)
        {

            //英数字と-,_,スペースのみ許可
            var regex = new Regex(@"^[a-zA-Z0-9-_ ]+$");
            return regex.IsMatch(name);
        }
    }
}
