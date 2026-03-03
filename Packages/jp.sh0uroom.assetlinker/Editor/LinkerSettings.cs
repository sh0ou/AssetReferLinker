using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace sh0uRoom.AssetLinker
{
    public sealed class LinkerSettings
    {
        private const string KeyLanguage = LinkerConstants.KeyPrefix + "language";
        private const string KeyAutoShow = LinkerConstants.KeyPrefix + "autoShow";

        // 対応言語
        private static readonly SystemLanguage[] supportLanguages = new SystemLanguage[]
        {
            SystemLanguage.English,
            SystemLanguage.Japanese
        };

        public static SystemLanguage[] GetLanguages() => supportLanguages;
        public static string[] GetLanguageStrings() => System.Array.ConvertAll(supportLanguages, x => x.ToString());

        // 言語（ユーザー毎）
        public static SystemLanguage Language
        {
            get
            {
                // 未設定時はOS言語を使用する
                var osLang = Application.systemLanguage;
                var defaultLang = System.Array.IndexOf(supportLanguages, osLang) != -1
                    ? osLang
                    : SystemLanguage.English;
                var stored = (SystemLanguage)EditorPrefs.GetInt(KeyLanguage, (int)defaultLang);
                // サポート外が保存されていたら英語にフォールバック
                return System.Array.IndexOf(supportLanguages, stored) == -1 ? SystemLanguage.English : stored;
            }
            set
            {
                var newValue = System.Array.IndexOf(supportLanguages, value) == -1 ? SystemLanguage.English : value;
                if (Language == newValue) return;
                EditorPrefs.SetInt(KeyLanguage, (int)newValue);
            }
        }

        // 自動表示（ユーザー毎）
        public static bool IsAutoShow
        {
            get => EditorPrefs.GetBool(KeyAutoShow, false);
            set
            {
                if (IsAutoShow == value) return;
                EditorPrefs.SetBool(KeyAutoShow, value);
            }
        }

        // Preferences に UI を提供
        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Preferences/AssetLinker", SettingsScope.User)
            {
                label = "AssetLinker",
                guiHandler = _ =>
                {
                    // 言語は対応言語のみの Popup を表示
                    var langs = GetLanguages();
                    var labels = GetLanguageStrings();
                    var currentIndex = System.Array.IndexOf(langs, Language);
                    if (currentIndex < 0) currentIndex = 0;

                    EditorGUI.BeginChangeCheck();
                    var nextIndex = EditorGUILayout.Popup("Language", currentIndex, labels);
                    var nextLang = langs[Mathf.Clamp(nextIndex, 0, langs.Length - 1)];
                    var nextAuto = EditorGUILayout.Toggle("Auto Show", IsAutoShow);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Language = nextLang;
                        IsAutoShow = nextAuto;
                    }

                    EditorGUILayout.Space();
                    if (EditorGUILayout.LinkButton("README"))
                    {
                        Application.OpenURL("https://github.com/sh0ou/AssetReferLinker/blob/main/README.md");
                    }
                },
                keywords = new HashSet<string>(new[] { "asset", "linker", "language", "auto", "show", "readme" })
            };
        }
    }
}