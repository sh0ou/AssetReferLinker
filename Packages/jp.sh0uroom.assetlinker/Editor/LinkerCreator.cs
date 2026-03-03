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

        private string _overridePath;
        private Vendor _vendor;

        // フォルダ選択時に入力済み値を保持するフィールド
        private string _savedDownloadURL;
        private string _savedAssetName;
        private string _savedLicenseURL;
        private bool _savedIsFree;

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
            var asset = linkerCreatorUxml.CloneTree();
            rootVisualElement.Add(asset);
            var rootView = asset.Q<VisualElement>("View");
            var loc = Localizer.Instance;

            var selectionPath = ResolveInitialSelectionPath();
            var fileName = string.IsNullOrEmpty(selectionPath) ? string.Empty
                                                                    : Path.GetFileName(selectionPath);

            var fileNameField = rootView.Q<TextField>("FileName");
            SetupReadOnlyTextField(fileNameField, loc.Translate("FILE_NAME"), fileName);
            rootView.Q<HelpBox>("FileNameWarning").text = loc.Translate("FILE_NAME_WARNING");

            var downloadURLField = rootView.Q<TextField>("DownloadURL");
            downloadURLField.label = loc.Translate("DOWNLOAD_URL");

            var filePathField = rootView.Q<TextField>("FilePath");
            SetupReadOnlyTextField(filePathField, loc.Translate("ASSET_PATH"), selectionPath);
            SetupFilePathButton(rootView.Q<Button>("FilePathButton"), downloadURLField, rootView);

            if (string.IsNullOrEmpty(selectionPath))
            {
                downloadURLField.style.display = DisplayStyle.None;
                return;
            }
            downloadURLField.style.display = DisplayStyle.Flex;

            var detailView = rootView.Q<VisualElement>("DetailView");
            SetupDownloadURLField(downloadURLField, detailView, detailView.Q<Label>("VendorInfo"));

            var assetNameField = SetupDetailTextField(detailView.Q<TextField>("AssetName"), loc.Translate("ASSET_NAME"), _savedAssetName);
            var licenseURLField = SetupDetailTextField(detailView.Q<TextField>("LicenseURL"), loc.Translate("LICENSE_URL"), _savedLicenseURL);
            SetupLicenseURLWarning(licenseURLField, detailView.Q<HelpBox>("LicenseURLWarning"), loc);

            var isFreeToggle = detailView.Q<Toggle>("IsFree");
            isFreeToggle.label = loc.Translate("ISFREE");
            if (_savedIsFree) isFreeToggle.value = true;

            // 保存値を復元（downloadURLField のコールバックが detailView の表示も更新する）
            if (_savedDownloadURL != null)
            {
                downloadURLField.value = _savedDownloadURL;
                ClearSavedState();
            }

            // パス一覧
            var filePathsFoldout = detailView.Q<Foldout>("FilePaths");
            filePathsFoldout.text = loc.Translate("FILEPATHS");
            var listContainer = filePathsFoldout.Q<ScrollView>().contentContainer;

            InitPathStateTables();
            var allPaths = CollectAndBuildPathHierarchy(selectionPath);
            BuildPathUI(allPaths, selectionPath, listContainer);

            detailView.Q<HelpBox>("ModifyWarning").text =
                $"<b><size=14>{loc.Translate("CREATE_WARNING_0")}</size></b>\n{loc.Translate("CREATE_WARNING_1")}";

            SetupCreateButton(rootView, fileNameField, downloadURLField, assetNameField, licenseURLField, isFreeToggle, loc);
            UpdateLinkStatusLabel(rootView.Q<Label>("LinkInfo"), fileNameField.value);
        }

        // ─────────────────────────────────────────────
        //  初期化ヘルパー
        // ─────────────────────────────────────────────

        /// <summary>
        /// 初期選択パスを解決する。
        /// _overridePath > Selection > ダイアログ の優先順で取得する。
        /// </summary>
        private string ResolveInitialSelectionPath()
        {
            var path = !string.IsNullOrEmpty(_overridePath)
                ? _overridePath
                : AssetDatabase.GetAssetPath(Selection.activeObject);
            _overridePath = null;

            if (!string.IsNullOrEmpty(path)) return path.Replace('\\', '/');

            var chosen = EditorUtility.OpenFolderPanel("Select Folder", "", "");
            if (string.IsNullOrEmpty(chosen)) return string.Empty;
            return ToUnityAssetPath(chosen)?.Replace('\\', '/') ?? string.Empty;
        }

        /// <summary>読み取り専用テキストフィールドにラベル・値・半透明を設定する。</summary>
        private static void SetupReadOnlyTextField(TextField field, string label, string value)
        {
            field.label = label;
            field.value = value;
            field.Q<VisualElement>(UI_TEXTBG_NAME).style.opacity = 0.5f;
        }

        /// <summary>詳細テキストフィールドにラベルと保存済み値を設定する。</summary>
        private static TextField SetupDetailTextField(TextField field, string label, string savedValue)
        {
            field.label = label;
            if (savedValue != null) field.value = savedValue;
            return field;
        }

        /// <summary>ファイルパス変更ボタンを配線する。</summary>
        private void SetupFilePathButton(Button button, TextField downloadURLField, VisualElement rootView)
        {
            button.clicked += () =>
            {
                var chosen = EditorUtility.OpenFolderPanel("Select Folder", "", "");
                if (string.IsNullOrEmpty(chosen)) return;

                var unityPath = ToUnityAssetPath(chosen);
                if (string.IsNullOrEmpty(unityPath))
                {
                    EditorUtility.DisplayDialog("AssetLinker", Localizer.Instance.Translate("FOLDER_NOT_IN_ASSETS"), "OK");
                    return;
                }
                unityPath = unityPath.Replace('\\', '/');

                SaveFormState(downloadURLField, rootView);
                AssetDatabase.Refresh();
                _overridePath = unityPath;

                var folderObj = AssetDatabase.LoadAssetAtPath<Object>(unityPath);
                if (folderObj != null) Selection.activeObject = folderObj;

                rootVisualElement.Clear();
                OnEnable();
            };
        }

        /// <summary>DownloadURL フィールドのコールバックを登録し、Vendor 判定を行う。</summary>
        private void SetupDownloadURLField(TextField downloadURLField, VisualElement detailView, Label vendorInfo)
        {
            _vendor = Vendor.Unknown;
            downloadURLField.RegisterValueChangedCallback(_ =>
            {
                var back = downloadURLField.Q<VisualElement>(UI_TEXTBG_NAME);
                var isValid = !string.IsNullOrEmpty(downloadURLField.value)
                              && downloadURLField.value.StartsWith("https://");

                detailView.style.display = isValid ? DisplayStyle.Flex : DisplayStyle.None;
                ColorUtility.TryParseHtmlString(isValid ? "#2A2A2A" : "#320000", out Color color);
                back.style.backgroundColor = color;

                if (isValid)
                    _vendor = UpdateVendorInfo(downloadURLField, vendorInfo);
            });
        }

        /// <summary>LicenseURL の変更時に警告 HelpBox の表示を切り替える。</summary>
        private static void SetupLicenseURLWarning(TextField licenseField, HelpBox warning, Localizer loc)
        {
            warning.text = loc.Translate("LICENSE_WARNING");
            licenseField.RegisterValueChangedCallback(_ =>
            {
                var isValid = !string.IsNullOrEmpty(licenseField.value)
                              && licenseField.value.StartsWith("https://");
                warning.style.display = isValid ? DisplayStyle.None : DisplayStyle.Flex;
            });
        }

        // ─────────────────────────────────────────────
        //  保存値の管理
        // ─────────────────────────────────────────────

        private void SaveFormState(TextField downloadURLField, VisualElement rootView)
        {
            _savedDownloadURL = downloadURLField.value;
            var dv = rootView.Q<VisualElement>("DetailView");
            _savedAssetName = dv?.Q<TextField>("AssetName")?.value;
            _savedLicenseURL = dv?.Q<TextField>("LicenseURL")?.value;
            _savedIsFree = dv?.Q<Toggle>("IsFree")?.value ?? false;
        }

        private void ClearSavedState()
        {
            _savedDownloadURL = null;
            _savedAssetName = null;
            _savedLicenseURL = null;
            _savedIsFree = false;
        }

        // ─────────────────────────────────────────────
        //  パス収集・ツリー構築
        // ─────────────────────────────────────────────

        private void InitPathStateTables()
        {
            _toggleStates = new Dictionary<string, bool>(256);
            _toggleByPath = new Dictionary<string, Toggle>(256);
            _childrenByFolder = new Dictionary<string, List<string>>(128);
            _parentByPath = new Dictionary<string, string>(512);
        }

        /// <summary>
        /// 選択パス配下のフォルダ/ファイルを収集し、親子関係マップを構築して
        /// ソート済み全パスリストを返す。
        /// </summary>
        private List<string> CollectAndBuildPathHierarchy(string selectionPath)
        {
            var allFolderPaths = new List<string>();
            var allFilePaths = new List<string>();

            if (AssetDatabase.IsValidFolder(selectionPath))
            {
                allFolderPaths.Add(selectionPath);
                CollectSubFoldersRecursive(selectionPath, allFolderPaths);

                var guids = AssetDatabase.FindAssets("", new[] { selectionPath });
                foreach (var g in guids)
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    if (!string.IsNullOrEmpty(p) && !AssetDatabase.IsValidFolder(p))
                        allFilePaths.Add(p);
                }
            }
            else
            {
                var guids = AssetDatabase.FindAssets("", new[] { selectionPath });
                if (guids != null && guids.Length > 0)
                {
                    foreach (var g in guids)
                    {
                        var p = AssetDatabase.GUIDToAssetPath(g);
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

            allFolderPaths.Sort(System.StringComparer.Ordinal);
            allFilePaths.Sort(System.StringComparer.Ordinal);

            var allPaths = new List<string>(allFolderPaths.Count + allFilePaths.Count);
            allPaths.AddRange(allFolderPaths);
            allPaths.AddRange(allFilePaths);

            foreach (var p in allPaths)
            {
                var parent = GetParentPath(p);
                if (string.IsNullOrEmpty(parent) || !parent.StartsWith(selectionPath)) continue;

                _parentByPath[p] = parent;
                if (!_childrenByFolder.TryGetValue(parent, out var list))
                {
                    list = new List<string>();
                    _childrenByFolder[parent] = list;
                }
                list.Add(p);
            }

            return allPaths;
        }

        /// <summary>パス一覧のトグル UI を生成する。</summary>
        private void BuildPathUI(List<string> allPaths, string selectionPath, VisualElement listContainer)
        {
            foreach (var p in allPaths)
            {
                var item = linkerCreatorItemUxml.CloneTree();
                var toggle = item.Q<VisualElement>("ItemView").Q<Toggle>("IsLink");

                toggle.SetValueWithoutNotify(true);
                toggle.showMixedValue = false;
                _toggleStates[p] = true;
                _toggleByPath[p] = toggle;
                toggle.Q<Label>("Path").text = p;

                toggle.RegisterValueChangedCallback(evt =>
                {
                    toggle.showMixedValue = false;
                    _toggleStates[p] = evt.newValue;

                    if (AssetDatabase.IsValidFolder(p))
                        SetChildrenStateRecursive(p, evt.newValue);

                    UpdateAncestorsState(p, selectionPath);
                });

                listContainer.Add(item);
            }
        }

        // ─────────────────────────────────────────────
        //  リンク作成ボタン
        // ─────────────────────────────────────────────

        private void SetupCreateButton(
            VisualElement rootView,
            TextField fileNameField,
            TextField downloadURLField,
            TextField assetNameField,
            TextField licenseURLField,
            Toggle isFreeToggle,
            Localizer loc)
        {
            rootView.Q<Button>("LinkButton").RegisterCallback<ClickEvent>(_ =>
            {
                var linkerData = new LinkerData
                {
                    Name = assetNameField.value,
                    FileName = fileNameField.value,
                    DownloadURL = downloadURLField.value,
                    LicenseURL = licenseURLField.value,
                    Vendor = _vendor,
                    IsFree = isFreeToggle.value,
                    Paths = _toggleStates.Where(x => x.Value).Select(x => x.Key).ToArray()
                };

                CreateLink(linkerData);

                if (IsLinked(linkerData.FileName))
                {
                    EditorUtility.DisplayDialog("LinkerCreator", loc.Translate("CREATE_OK"), "OK");
                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog("LinkerCreator", loc.Translate("CREATE_NG"), "OK");
                }
            });
        }

        private static void UpdateLinkStatusLabel(Label label, string fileName)
        {
            if (IsLinked(fileName))
            {
                label.text = "Linked!";
                label.style.color = Color.green;
            }
            else
            {
                label.text = "Not Linked";
                label.style.color = Color.yellow;
            }
        }

        private void OnDisable()
        {
            rootVisualElement.Clear();
        }

        private static bool IsLinked(string fileName)
        {
            var path = LinkerFileUtil.GetLinkPath(fileName);
            return LinkerFileUtil.TryReadJson(path, out LinkerData _);
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

    }
}
