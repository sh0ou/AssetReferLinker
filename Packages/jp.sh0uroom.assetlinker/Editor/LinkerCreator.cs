using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);

            var downloadURLField = rootView.Q<TextField>("DownloadURL");
            downloadURLField.label = Localizer.Instance.Translate("DOWNLOAD_URL");
            if (string.IsNullOrEmpty(path))
            {
                downloadURLField.style.display = DisplayStyle.None;
                return;
            }
            else
            {
                downloadURLField.style.display = DisplayStyle.Flex;
            }
            var filePathField = rootView.Q<TextField>("FilePath");
            filePathField.label = Localizer.Instance.Translate("ASSET_PATH");
            filePathField.value = path;

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
            assetNameField.label = Localizer.Instance.Translate("ASSET_NAME");
            assetNameField.RegisterValueChangedCallback((evt) =>
            {
                OnAssetNameChanged(detailView, assetNameField);
            });

            //ライセンスURL
            var licenseURLField = detailView.Q<TextField>("LicenseURL");
            licenseURLField.label = Localizer.Instance.Translate("LICENSE_URL");
            var licenseURLWarning = detailView.Q<HelpBox>("LicenseURLWarning");
            licenseURLWarning.text = Localizer.Instance.Translate("LICENSE_WARNING");
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
            isFreeToggle.label = Localizer.Instance.Translate("ISFREE");

            //ファイルパス
            var filePathsFoldout = detailView.Q<Foldout>("FilePaths");
            filePathsFoldout.text = Localizer.Instance.Translate("FILEPATHS");
            var filePaths = AssetDatabase.FindAssets("", new string[] { path }); //GUIDなので後でパスに変換する
            var fileToggles = new Dictionary<string, bool>();
            for (int i = 0; i < filePaths.Length; i++)
            {
                var filePath = filePaths[i];
                var pathStr = AssetDatabase.GUIDToAssetPath(filePath); //これをtargetPathsに追加する
                var item = linkerCreatorItemUxml.CloneTree();
                var toggleField = item.Q<VisualElement>("ItemView").Q<Toggle>("IsLink");
                fileToggles.Add(pathStr, true);
                toggleField.RegisterValueChangedCallback((evt) =>
                {
                    if (evt.newValue)
                    {
                        fileToggles[pathStr] = true;
                    }
                    else
                    {
                        fileToggles[pathStr] = false;
                    }
                });

                toggleField.Q<Label>("Path").text = pathStr;
                var container = filePathsFoldout.Q<ScrollView>().contentContainer;
                container.Add(item);
            }

            //警告文
            var modifyWarning = detailView.Q<HelpBox>("ModifyWarning");
            modifyWarning.text = $"<b><size=14>{Localizer.Instance.Translate("CREATE_WARNING_0")}</size></b>\n{Localizer.Instance.Translate("CREATE_WARNING_1")}";

            //リンク作成
            var createButton = rootView.Q<Button>("LinkButton");

            createButton.RegisterCallback<ClickEvent>((evt) =>
            {
                var linkerData = new LinkerData
                {
                    Name = assetNameField.value,
                    DownloadURL = downloadURLField.value,
                    LicenseURL = licenseURLField.value,
                    Vendor = vendor,
                    IsFree = isFreeToggle.value,
                    Paths = fileToggles.Where(x => x.Value).Select(x => x.Key).ToArray()
                };
                CreateLink(linkerData);
                if (ValidateFile(linkerData.Name))
                {
                    // ポップアップ表示
                    var message = Localizer.Instance.Translate("CREATE_OK");
                    EditorUtility.DisplayDialog("LinkerCreator", message, "OK");
                    Close();
                }
                else
                {
                    var message = Localizer.Instance.Translate("CREATE_NG");
                    EditorUtility.DisplayDialog("LinkerCreator", message, "OK");
                }
            });

            //リンク情報
            var linkInfo = rootView.Q<Label>("LinkInfo");
            if (ValidateFile(assetNameField.value))
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

        private void OnAssetNameChanged(VisualElement detailView, TextField assetNameField)
        {
            var back = assetNameField.Q<VisualElement>(UI_TEXTBG_NAME);
            var warningView = detailView.Q<HelpBox>("AssetNameWarning");
            warningView.text = Localizer.Instance.Translate("ASSETNAME_WARNING");
            if (string.IsNullOrEmpty(assetNameField.value))
            {
                ColorUtility.TryParseHtmlString("#320000", out Color color);
                back.style.backgroundColor = color;
                warningView.style.display = DisplayStyle.None;
                return;
            }
            else
            {
                ColorUtility.TryParseHtmlString("#2A2A2A", out Color color);
                back.style.backgroundColor = color;
            }

            if (!ValidateAssetName(assetNameField.value))
            {
                warningView.style.display = DisplayStyle.Flex;
            }
            else
            {
                warningView.style.display = DisplayStyle.None;
            }
        }

        private void OnDisable()
        {
            rootVisualElement.Clear();
        }

        private static bool ValidateFile(string name)
        {
            var path = $"AssetLinker/{name}.astlnk";
            return System.IO.File.Exists(path) && System.IO.File.ReadAllText(path).Contains("DownloadURL");
        }

        private void CreateLink(LinkerData data)
        {
            try
            {
                var path = $"{FOLDER_NAME}/{data.Name}.astlnk";

                // JSON形式で出力
                // var json = JsonUtility.ToJson(data);
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                // Debug.Log("Generated JSON: " + json);

                // ファイルにJSONを書き込む
                if (!System.IO.Directory.Exists(FOLDER_NAME))
                {
                    System.IO.Directory.CreateDirectory(FOLDER_NAME);
                }
                System.IO.File.WriteAllText(path, json);
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

            if (TryUpdateVendorLabel(downloadURLField.value, LinkerInfo.vketStoreURLs, labelField, Vendor.VKetStore))
                return Vendor.VKetStore;

            labelField.text = Localizer.Instance.Translate("VENDOR_UNKNOWN");
            labelField.style.color = Color.yellow;
            return Vendor.Unknown;
        }

        private static bool TryUpdateVendorLabel(string url, IEnumerable<string> targetUrls, Label labelField, Vendor vendor)
        {
            foreach (var targetUrl in targetUrls)
            {
                var pattern = Regex.Escape(targetUrl).Replace("\\*", ".*");
                if (Regex.IsMatch(url, pattern))
                {
                    var info0 = Localizer.Instance.Translate("VENDOR_INFO_0");
                    var info1 = Localizer.Instance.Translate("VENDOR_INFO_1");
                    labelField.text = $"{info0} {vendor} {info1}";
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
        private const string FOLDER_NAME = "AssetLinker";
        private const string UI_TEXTBG_NAME = "unity-text-input";
    }
}
