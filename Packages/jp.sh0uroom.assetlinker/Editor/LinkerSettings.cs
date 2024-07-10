using UnityEngine;

namespace sh0uRoom.AssetLinker
{
    public class LinkerSettings : ScriptableObject
    {
        private SystemLanguage language;

        public SystemLanguage Language
        {
            get => language;
            set
            {
                //English,Japanese以外の場合はログを出してEnglishに設定
                if (value != SystemLanguage.English &&
                    value != SystemLanguage.Japanese)
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

        public bool IsAutoShowMissing = true;
    }
}