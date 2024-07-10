using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace sh0uRoom.AssetLinker
{
    public class Localizer : Editor
    {
        public void LoadLocalization()
        {
            Debug.Log("LoadLocalization");
            localizationDic = new Dictionary<string, Dictionary<SystemLanguage, string>>();

            var lines = csv.text.Split('\n');
            if (lines.Length <= 1) return;

            var headers = lines[0].Split(',');

            for (var i = 0; i < lines.Length; i++)
            {
                var fields = lines[i].Split(',');
                if (fields.Length < headers.Length) continue;

                var id = fields[0];
                if (!localizationDic.ContainsKey(id))
                {
                    localizationDic[id] = new Dictionary<SystemLanguage, string>();
                }

                for (var j = 1; j < headers.Length; j++)
                {
                    if (System.Enum.TryParse(headers[j], out SystemLanguage language))
                    {
                        localizationDic[id][language] = fields[j];
                    }
                }
            }
        }

        public string Translate(string id)
        {
            if (localizationDic.TryGetValue(id, out var dic))
            {
                if (dic.TryGetValue(settings.Language, out var text))
                {
                    return text;
                }
                else
                {
                    return $"Missing: {settings.Language}";
                }
            }
            return $"Missing: {id}";
        }

        [SerializeField] private TextAsset csv;
        [SerializeField] private LinkerSettings settings;
        private Dictionary<string, Dictionary<SystemLanguage, string>> localizationDic;
    }
}