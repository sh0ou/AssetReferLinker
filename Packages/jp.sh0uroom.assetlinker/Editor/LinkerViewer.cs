using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
            var loc = Localizer.Instance;

            if (LinkerFileUtil.DirectoryExists(LinkerConstants.FolderName))
            {
                var linkerPaths = LinkerFileUtil.GetAllLinkPaths();
                foreach (var path in linkerPaths)
                {
                    if (!LinkerFileUtil.TryReadJson(path, out LinkerData linker) || linker == null)
                    {
                        continue;
                    }

                    var itemUxml = linkerViewerItemUxml.CloneTree();
                    var itemView = itemUxml.Q<VisualElement>("ItemView");

                    itemView.Q<Foldout>().text = linker.Name;
                    var container = itemView.contentContainer;

                    var vendorField = container.Q<Label>("VendorInfo");

                    var str_free = loc.Translate("FREE");
                    var str_paid = loc.Translate("PAID");
                    vendorField.text = $"{linker.Vendor} / {(linker.IsFree ? str_free : str_paid)}";

                    var actionView = container.Q<VisualElement>("ActionView");

                    // ダウンロードURLがある場合は表示
                    var downloadButton = actionView.Q<Button>("Download");
                    downloadButton.clicked += () =>
                    {
                        var url = linker.DownloadURL;
                        var message = loc.Translate("OPENURL_MESSAGE");
                        if (EditorUtility.DisplayDialog("Open URL", $"{message}\n{url}", "Yes", "No"))
                        {
                            Application.OpenURL(url);
                        }
                    };

                    // ライセンスページを開くボタン
                    var licenseButton = actionView.Q<Button>("License");
                    licenseButton.SetEnabled(!string.IsNullOrEmpty(linker.LicenseURL));
                    licenseButton.clicked += () =>
                    {
                        var url = linker.LicenseURL;
                        var message = loc.Translate("OPENURL_MESSAGE");
                        if (EditorUtility.DisplayDialog("Open URL", $"{message}\n{url}", "Yes", "No"))
                        {
                            Application.OpenURL(url);
                        }
                    };

                    // リンク解除ボタン
                    var unlinkButton = actionView.Q<Button>("Unlink");
                    unlinkButton.clicked += () =>
                    {
                        var message = loc.Translate("UNLINK_MESSAGE");
                        if (EditorUtility.DisplayDialog("Unlink", message, "Yes", "No"))
                        {
                            itemRootView.Remove(itemUxml);
                            System.IO.File.Delete(path);
                            LinkerProjectWindowDecorator.NotifyLinksChanged();
                        }
                    };

                    var isMissingFound = false;
                    var pathsView = container.Q<Foldout>("Paths").Q<ScrollView>();
                    foreach (var assetPath in linker.Paths)
                    {
                        var pathLabel = new Label(assetPath);
                        if (!LinkerFileUtil.FileExists(assetPath) && !LinkerFileUtil.DirectoryExists(assetPath))
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

                    var color = isMissingFound ? new Color(1f, 1f, 0f, 0.5f) : new Color(0f, 1f, 0f, 0.5f);
                    var fold = itemView.Q<Foldout>();
                    fold.style.borderTopColor = color;
                    fold.style.borderBottomColor = color;
                    fold.style.borderLeftColor = color;
                    fold.style.borderRightColor = color;

                    itemRootView.Add(itemUxml);
                }

                if (linkerPaths.Length == 0)
                {
                    missingInfo.text = loc.Translate("MISSINGINFO_ERROR");
                    missingInfo.style.color = Color.yellow;
                }
                else if (missingCount > 0)
                {
                    missingInfo.text = $"{missingCount} {loc.Translate("MISSINGINFO_FOUND")}";
                    missingInfo.style.color = Color.yellow;
                }
                else
                {
                    missingInfo.text = loc.Translate("MISSINGINFO_OK");
                    missingInfo.style.color = Color.green;
                }
            }
            else
            {
                missingInfo.text = loc.Translate("MISSINGINFO_ERROR");
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

        [SerializeField] private VisualTreeAsset linkerViewerUxml;
        [SerializeField] private VisualTreeAsset linkerViewerItemUxml;
    }

    [InitializeOnLoad]
    public class LinkerViewerLoader
    {
        static LinkerViewerLoader() => EditorApplication.delayCall += ShowOnStartup;

        static void ShowOnStartup()
        {
            var isAlreadyShown = SessionState.GetBool(LinkerConstants.AlreadyShownSessionKey, false);
            if (!isAlreadyShown)
            {
                OnLinkerViewerLoader();
                SessionState.SetBool(LinkerConstants.AlreadyShownSessionKey, true);
            }
        }

        static void OnLinkerViewerLoader()
        {
            if (!LinkerFileUtil.DirectoryExists(LinkerConstants.FolderName)) return;

            Debug.Log("Validate Assets...");

            // missing がある場合はウインドウを開く
            var linkerPaths = LinkerFileUtil.GetAllLinkPaths();
            foreach (var path in linkerPaths)
            {
                if (!LinkerFileUtil.TryReadJson(path, out LinkerData linker) || linker == null)
                {
                    continue;
                }

                foreach (var p in linker.Paths)
                {
                    if (!LinkerFileUtil.FileExists(p) && !LinkerFileUtil.DirectoryExists(p))
                    {
                        LinkerViewer.CreateWindow();
                        return;
                    }
                }
            }
        }
    }
}
