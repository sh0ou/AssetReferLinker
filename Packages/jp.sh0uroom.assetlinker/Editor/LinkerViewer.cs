using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;

namespace sh0uRoom.AssetLinker
{
    public class LinkerViewer : EditorWindow
    {
        [MenuItem("Window/AssetLinker")]
        public static void CreateWindow()
        {
            var window = GetWindow<LinkerViewer>();
            window.titleContent = new GUIContent("LinkerViewer");
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // missingがある場合はウインドウを開く
            var linkerDatas = System.IO.Directory.GetFiles("AssetLinker", "*.astlnk");
            var missingCount = 0;
            foreach (var data in linkerDatas)
            {
                var linker = JsonConvert.DeserializeObject<LinkerData>(System.IO.File.ReadAllText(data));
                foreach (var path in linker.Paths)
                {
                    if (!System.IO.File.Exists(path) && !System.IO.Directory.Exists(path))
                    {
                        missingCount++;
                        break;
                    }
                }
            }

            if (missingCount > 0)
            {
                CreateWindow();
            }
        }

        private void OnEnable()
        {
            var rootUxml = linkerViewerUxml.CloneTree();
            rootVisualElement.Add(rootUxml);
            var itemRootView = rootUxml.Q<ScrollView>();

            var missingCount = 0;
            var linkerDatas = System.IO.Directory.GetFiles("AssetLinker", "*.astlnk");
            foreach (var data in linkerDatas)
            {
                var linker = LoadLinkerData(data);
                if (linker == null)
                {
                    continue;
                }

                var itemUxml = linkerViewerItemUxml.CloneTree();
                var itemView = itemUxml.Q<VisualElement>("ItemView");

                itemView.Q<Foldout>().text = linker.Name;
                var container = itemView.contentContainer;

                var vendorField = container.Q<Label>("VendorInfo");
                vendorField.text = $"{linker.Vendor} / {(linker.IsFree ? "Free" : "Paid")}";

                var downloadButton = container.Q<VisualElement>("ActionView").Q<Button>("Download");
                downloadButton.clicked += () =>
                {
                    var url = linker.DownloadURL;
                    Application.OpenURL(url);
                };
                var licenseButton = container.Q<VisualElement>("ActionView").Q<Button>("License");
                licenseButton.clicked += () =>
                {
                    var url = linker.LicenseURL;
                    Application.OpenURL(url);
                };
                var unlinkButton = container.Q<VisualElement>("ActionView").Q<Button>("Unlink");
                unlinkButton.clicked += () =>
                {
                    if (EditorUtility.DisplayDialog("Unlink", "Are you sure to unlink this asset?", "Yes", "No"))
                    {
                        itemRootView.Remove(itemUxml);
                        System.IO.File.Delete(data);
                    }
                };

                var isMissingFound = false;
                var pathsView = container.Q<Foldout>("Paths").Q<ScrollView>();
                foreach (var path in linker.Paths)
                {
                    var pathLabel = new Label(path);
                    //存在しないパスまたはディレクトリは黄色で表示
                    if (!ValidateFile(path) && !ValidateDirectory(path))
                    {
                        pathLabel.style.color = Color.yellow;
                        missingCount++;
                        isMissingFound = true;
                    }
                    else
                    {
                        pathLabel.style.color = Color.green;
                    }

                    pathsView.contentContainer.Add(pathLabel);
                }
                if (isMissingFound)
                {
                    var color = new Color(1f, 1f, 0f, 0.5f);
                    itemView.Q<Foldout>().style.borderTopColor = color;
                    itemView.Q<Foldout>().style.borderBottomColor = color;
                    itemView.Q<Foldout>().style.borderLeftColor = color;
                    itemView.Q<Foldout>().style.borderRightColor = color;
                }
                else
                {
                    var color = new Color(0f, 1f, 0f, 0.5f);
                    itemView.Q<Foldout>().style.borderTopColor = color;
                    itemView.Q<Foldout>().style.borderBottomColor = color;
                    itemView.Q<Foldout>().style.borderLeftColor = color;
                    itemView.Q<Foldout>().style.borderRightColor = color;
                }

                itemRootView.Add(itemUxml);
            }

            var missingInfo = rootUxml.Q<Label>("MissingInfo");
            if (linkerDatas.Length == 0)
            {
                missingInfo.text = "No linker data found.";
                missingInfo.style.color = Color.yellow;
            }
            else if (missingCount > 0)
            {
                missingInfo.text = $"{missingCount} Asset is missing";
                missingInfo.style.color = Color.yellow;
            }
            else
            {
                missingInfo.text = "All Asset is imported!";
                missingInfo.style.color = Color.green;
            }
        }

        private void OnDisable()
        {
            rootVisualElement.Clear();
        }

        private LinkerData LoadLinkerData(string path)
        {
            // var path = $"AssetLinker/{name}.astlnk";
            if (!ValidateFile(path))
            {
                return null;
            }

            var json = System.IO.File.ReadAllText(path);
            return JsonConvert.DeserializeObject<LinkerData>(json);
        }

        private bool ValidateFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            if (!System.IO.File.Exists(path))
            {
                return false;
            }
            return true;
        }

        private bool ValidateDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            if (!System.IO.Directory.Exists(path))
            {
                return false;
            }
            return true;
        }

        [SerializeField] private VisualTreeAsset linkerViewerUxml;
        [SerializeField] private VisualTreeAsset linkerViewerItemUxml;
    }
}
