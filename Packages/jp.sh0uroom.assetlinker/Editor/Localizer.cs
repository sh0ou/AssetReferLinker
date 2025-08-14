using System.Collections.Generic;
using UnityEngine;

namespace sh0uRoom.AssetLinker
{
    public class Localizer : SingletonEditor<Localizer>
    {
        public void LoadLocalization()
        {
            if (localizationDic != null && localizationDic.Count > 0) return;
            localizationDic = new Dictionary<string, Dictionary<SystemLanguage, string>>();

            if (csv == null || string.IsNullOrEmpty(csv.text))
            {
                Debug.LogWarning("Localization CSV is missing or empty.");
                return;
            }

            var lines = csv.text.Split('\n');
            if (lines.Length <= 1) return;

            var headers = lines[0].TrimEnd('\r').Split(',');

            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = line.TrimEnd('\r').Split(',');
                if (fields.Length < headers.Length) continue;

                var id = fields[0];
                if (string.IsNullOrEmpty(id)) continue;

                if (!localizationDic.ContainsKey(id))
                {
                    localizationDic[id] = new Dictionary<SystemLanguage, string>();
                }

                for (var j = 1; j < headers.Length && j < fields.Length; j++)
                {
                    SystemLanguage language;
                    if (System.Enum.TryParse(headers[j], out language))
                    {
                        localizationDic[id][language] = fields[j];
                    }
                }
            }
        }

        public string Translate(string id)
        {
            LoadLocalization();
            if (localizationDic != null && localizationDic.TryGetValue(id, out var dic))
            {
                string text;
                if (dic.TryGetValue(LinkerSettings.Language, out text))
                {
                    return text;
                }
                // フォールバック：英語 -> 先頭の値
                if (dic.TryGetValue(SystemLanguage.English, out text))
                {
                    return text;
                }
                foreach (var kv in dic)
                {
                    return kv.Value;
                }
            }
            return $"Missing: {id}";
        }

        [SerializeField] private TextAsset csv;
        private Dictionary<string, Dictionary<SystemLanguage, string>> localizationDic;
    }
}
