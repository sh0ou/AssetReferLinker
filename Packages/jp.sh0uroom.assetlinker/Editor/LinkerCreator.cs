using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;

namespace sh0uRoom.AssetLinker
{
    public class LinkerCreator : EditorWindow
    {
        [MenuItem("Assets/AssetLinker/LinkerCreator")]
        public static void Create()
        {
            var window = GetWindow<LinkerCreator>();
        }

        private void OnEnable()
        {
            //ルート要素の作成
            var asset = linkerCreatorAsset.CloneTree();
            rootVisualElement.Add(asset);
            var rootView = asset.Q<VisualElement>("View");
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);

            var filePathField = rootView.Q<TextField>("FilePath");
            filePathField.value = path;

            //ダウンロードURL
            var downloadURLField = rootView.Q<TextField>("DownloadURL");
            var detailView = rootView.Q<VisualElement>("DetailView");

            var vendorInfo = detailView.Q<Label>("VendorInfo");
            var vendor = Vendor.Unknown;
            downloadURLField.RegisterValueChangedCallback((evt) =>
            {
                if (!string.IsNullOrEmpty(downloadURLField.value) && downloadURLField.value.StartsWith("https://"))
                {
                    detailView.style.display = DisplayStyle.Flex;
                    vendor = UpdateVendorInfo(downloadURLField, vendorInfo);
                }
                else
                {
                    detailView.style.display = DisplayStyle.None;
                }
            });

            //ライセンスURL
            var licenseURLField = detailView.Q<TextField>("LicenseURL");
            var licenseURLWarning = detailView.Q<HelpBox>("LicenseURLWarning");
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

            //ファイルパス
            var filePathsFoldout = detailView.Q<Foldout>("FilePaths");
            var filePaths = AssetDatabase.FindAssets("", new string[] { path });
            foreach (var filePath in filePaths)
            {
                var pathStr = AssetDatabase.GUIDToAssetPath(filePath);
                var item = linkerItemAsset.CloneTree();
                var toggleField = item.Q<VisualElement>("ItemView").Q<Toggle>("IsLink");
                toggleField.Q<Label>("Path").text = pathStr;
                filePathsFoldout.contentContainer.Add(item);
            }

            //リンク作成
            var createButton = rootView.Q<Button>("LinkButton");
            createButton.RegisterCallback<ClickEvent>((evt) =>
            {
                var linkerData = new LinkerData(downloadURLField.value, licenseURLField.value, vendor, isFreeToggle.value, filePaths);
                if (CreateLink(linkerData))
                {
                    Debug.Log("Link created!");
                }
                else
                {
                    Debug.LogError("Failed to create link");
                }
            });

            //リンク情報
            var linkInfo = rootView.Q<Label>("LinkInfo");
            if (System.IO.File.Exists($"{Application.productName}.astlnk"))
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

        private bool CreateLink(LinkerData data)
        {
            try
            {
                var exportPath = $"{Application.productName}.astlnk";

                //JSON形式で出力
                var json = JsonUtility.ToJson(data);
                System.IO.File.WriteAllText(exportPath, json);

                //ファイルが存在するか確認
                if (!System.IO.File.Exists(exportPath))
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
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

            labelField.text = "Unknown URL";
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
                    labelField.text = $"This is {vendor} URL";
                    labelField.style.color = Color.green;
                    return true;
                }
            }
            return false;
        }

        [SerializeField] private VisualTreeAsset linkerCreatorAsset;
        [SerializeField] private VisualTreeAsset linkerItemAsset;
    }
}
