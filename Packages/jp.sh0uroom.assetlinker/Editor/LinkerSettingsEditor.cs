using UnityEngine;
using UnityEditor;

namespace sh0uRoom.AssetLinker
{
    [CustomEditor(typeof(LinkerSettings))]
    public class LinkerSettingsEditor : Editor
    {
        [MenuItem("Window/AssetLinker/Settings")]
        public static void OpenSettings()
        {
            var file = AssetDatabase.FindAssets("t:LinkerSettings")[0];
            var settings = AssetDatabase.LoadAssetAtPath<LinkerSettings>(AssetDatabase.GUIDToAssetPath(file));
            if (settings)
            {
                Selection.activeObject = settings;
            }
            else
            {
                Debug.LogWarning("Settings not found. Please Reimport Package.");
            }
        }

        public override void OnInspectorGUI()
        {
            var settings = target as LinkerSettings;
            if (settings == null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            var enableLanguages = LinkerSettings.GetLanguages();
            var languageIndex = EditorGUILayout.Popup("Language", System.Array.IndexOf(enableLanguages, settings.Language), LinkerSettings.GetLanguageStrings());
            settings.Language = enableLanguages[languageIndex];

            settings.IsAutoShowMissing = EditorGUILayout.Toggle("Auto Show Missing", settings.IsAutoShowMissing);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
