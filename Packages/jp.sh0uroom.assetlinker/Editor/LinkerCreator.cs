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
                var chosen = EditorUtility.OpenFolderPanel("Select Folder", "", "");
                if (!string.IsNullOrEmpty(chosen))
                {
                    fileName = Path.GetFileName(chosen);
                    selectionPath = chosen.Replace('\\', '/');
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

            // ファイルパス
            var filePathsFoldout = detailView.Q<Foldout>("FilePaths");
            filePathsFoldout.text = loc.Translate("FILEPATHS");
            var guids = AssetDatabase.FindAssets("", new string[] { selectionPath }); // GUID を後でパスに変換
            var fileToggles = new Dictionary<string, bool>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                var guid = guids[i];
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var item = linkerCreatorItemUxml.CloneTree();
                var toggleField = item.Q<VisualElement>("ItemView").Q<Toggle>("IsLink");
                fileToggles.Add(assetPath, true);
                toggleField.RegisterValueChangedCallback(evt =>
                {
                    fileToggles[assetPath] = evt.newValue;
                });

                toggleField.Q<Label>("Path").text = assetPath;
                filePathsFoldout.Q<ScrollView>().contentContainer.Add(item);
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
                    Paths = fileToggles.Where(x => x.Value).Select(x => x.Key).ToArray()
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
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
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

        [SerializeField] private VisualTreeAsset linkerCreatorUxml;
        [SerializeField] private VisualTreeAsset linkerCreatorItemUxml;
        private const string UI_TEXTBG_NAME = "unity-text-input";
    }
}
