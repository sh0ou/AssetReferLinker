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
            var settings = LinkerSettings.instance;
            if (settings)
            {
                Selection.activeObject = settings;
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(settings);
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

            settings.IsAutoShow = EditorGUILayout.Toggle("Auto Show Window", settings.IsAutoShow);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
