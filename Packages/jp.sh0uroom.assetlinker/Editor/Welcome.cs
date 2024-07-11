using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace sh0uRoom.AssetLinker
{
    [InitializeOnLoad]
    public class Welcome : EditorWindow
    {
        static Welcome() => EditorApplication.delayCall += Check;

        private static void Check()
        {
            var isAlreadyShown = SessionState.GetBool("sh0uRoom.AssetLinker.Welcome", false);
            if (!isAlreadyShown)
            {
                ShowWindow();
                SessionState.SetBool("sh0uRoom.AssetLinker.Welcome", true);
            }
        }

        [MenuItem("Window/AssetLinker/Welcome")]
        private static void ShowWindow()
        {
            GetWindow<Welcome>().Show();
        }

        private void OnEnable()
        {
            var window = GetWindow<Welcome>();
            window.titleContent = new GUIContent("Welcome");
            OnShow();
        }

        private void OnDisable()
        {
            rootVisualElement.Clear();
        }

        private void OnShow()
        {
            var rootUxml = welcomeUxml.CloneTree();
            rootVisualElement.Add(rootUxml);

            var readmeButton = rootUxml.Q<Button>("Readme");
            readmeButton.clicked += () => Application.OpenURL("https://github.com/sh0ou/AssetReferLinker/blob/main/README.md");

            var settingsButton = rootUxml.Q<Button>("Settings");
            settingsButton.clicked += () => LinkerSettingsEditor.OpenSettings();
        }

        [SerializeField] private VisualTreeAsset welcomeUxml;
    }
}
