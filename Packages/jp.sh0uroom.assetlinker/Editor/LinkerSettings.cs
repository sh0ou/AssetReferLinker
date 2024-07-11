using UnityEditor;
using UnityEngine;

namespace sh0uRoom.AssetLinker
{
    [FilePath("Packages/jp.sh0uroom.assetlinker/Editor/Settings.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class LinkerSettings : ScriptableSingleton<LinkerSettings>
    {
        private SystemLanguage language;

        public SystemLanguage Language
        {
            get => language;
            set
            {
                if (System.Array.IndexOf(GetLanguages(), value) == -1)
                {
                    Debug.LogWarning("Language is not supported. Set to English.");
                    language = SystemLanguage.English;
                }
                else
                {
                    language = value;
                }
            }
        }

        private bool isAutoShow;
        public bool IsAutoShow
        {
            get => isAutoShow;
            set => isAutoShow = value;

        }

        public void OnEnable()
        {
            if (language == default)
            {
                language = SystemLanguage.English;
            }
            if (isAutoShow == default)
            {
                isAutoShow = true;
            }
        }

        private static SystemLanguage[] supportLanguages = new SystemLanguage[]
        {
            SystemLanguage.English,
            SystemLanguage.Japanese
        };
        public static SystemLanguage[] GetLanguages() => supportLanguages;
        public static string[] GetLanguageStrings() => System.Array.ConvertAll(supportLanguages, x => x.ToString());
    }
}