using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;

namespace sh0uRoom.AssetLinker
{
    public class LinkerViewer : EditorWindow
    {
        [MenuItem("Window/AssetLinker/Show Linker")]
        public static void CreateWindow()
        {
            var window = GetWindow<LinkerViewer>();
            window.titleContent = new GUIContent("LinkerViewer");
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDisable()
        {
            rootVisualElement.Clear();
        }

        private void Refresh()
        {
            var rootUxml = linkerViewerUxml.CloneTree();
            rootVisualElement.Add(rootUxml);
            var itemRootView = rootUxml.Q<ScrollView>();

            var missingCount = 0;
            var missingInfo = rootUxml.Q<Label>("MissingInfo");

            //ディレクトリが存在するか確認
            if (System.IO.Directory.Exists("AssetLinker"))
            {
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
                        if (EditorUtility.DisplayDialog("Open Download URL", $"Open this URL?\n{url}", "Yes", "No"))
                        {
                            Application.OpenURL(url);
                        }
                    };
                    var licenseButton = container.Q<VisualElement>("ActionView").Q<Button>("License");
                    if (string.IsNullOrEmpty(linker.LicenseURL))
                    {
                        licenseButton.SetEnabled(false);
                    }
                    else
                    {
                        licenseButton.SetEnabled(true);
                    }
                    licenseButton.clicked += () =>
                    {
                        var url = linker.LicenseURL;
                        if (EditorUtility.DisplayDialog("Open Download URL", $"Open this URL?\n{url}", "Yes", "No"))
                        {
                            Application.OpenURL(url);
                        }
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
                            pathLabel.style.color = Color.red;
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
            else
            {
                missingInfo.text = "No linker directory found.";
                missingInfo.style.color = Color.yellow;
            }

            var dontShowField = rootUxml.Q<Toggle>("DontShow");
            dontShowField.RegisterValueChangedCallback((evt) =>
            {
                if (evt.newValue)
                {
                    settings.IsAutoShowMissing = false;
                }
                else
                {
                    settings.IsAutoShowMissing = true;
                }
            });
        }

        private LinkerData LoadLinkerData(string path)
        {
            // var path = $"AssetLinker/{name}.astlnk";
            if (!ValidateFile(path)) return null;

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
        [SerializeField] private LinkerSettings settings;
    }

    [InitializeOnLoad]
    public class LinkerViewerLoader
    {
        static LinkerViewerLoader() => EditorApplication.delayCall += ShowOnStartup;

        static void ShowOnStartup()
        {
            var isAlreadyShown = SessionState.GetBool(AlreadyShown, false);
            if (!isAlreadyShown)
            {
                OnLinkerViewerLoader();
                SessionState.SetBool(AlreadyShown, true);
            }
        }

        static void OnLinkerViewerLoader()
        {
            if (!System.IO.Directory.Exists("AssetLinker")) return;

            Debug.Log("Validate Assets...");

            // missingがある場合はウインドウを開く
            var linkerDatas = System.IO.Directory.GetFiles("AssetLinker", "*.astlnk");
            var isHasmissing = false;
            foreach (var data in linkerDatas)
            {
                var linker = JsonConvert.DeserializeObject<LinkerData>(System.IO.File.ReadAllText(data));
                foreach (var path in linker.Paths)
                {
                    if (!System.IO.File.Exists(path) && !System.IO.Directory.Exists(path))
                    {
                        isHasmissing = true;
                        break;
                    }
                }
            }

            if (isHasmissing) LinkerViewer.CreateWindow();
        }

        [SerializeField] private Localizer localizer;
        private const string AlreadyShown = "com.example.welcome_window_shown";
    }
}
