using UnityEditor;
using UnityEngine;

namespace sh0uRoom.AssetLinker
{
    [FilePath("Packages/jp.sh0uroom.assetlinker/Editor/Settings.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class LinkerSettings : ScriptableSingleton<LinkerSettings>
    {
        [SerializeField]
        private SystemLanguage language = SystemLanguage.English;

        public SystemLanguage Language
        {
            get => language;
            set
            {
                var supported = GetLanguages();
                var newValue = System.Array.IndexOf(supported, value) == -1 ? SystemLanguage.English : value;
                if (language == newValue) return;
                language = newValue;
                Save(true); // 変更時に保存
            }
        }

        private bool isAutoShow;
        public bool IsAutoShow
        {
            get => isAutoShow;
            set
            {
                if (isAutoShow == value) return;
                isAutoShow = value;
                Save(true); // 変更時に保存
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