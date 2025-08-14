using System.Collections.Generic;
using UnityEngine;

namespace sh0uRoom.AssetLinker
{
    public class Localizer : SingletonEditor<Localizer>
    {
        public void LoadLocalization()
        {
            // Debug.Log("LoadLocalization");
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
            LoadLocalization();
            if (localizationDic.TryGetValue(id, out var dic))
            {
                if (dic.TryGetValue(LinkerSettings.Language, out var text))
                {
                    return text;
                }
                else
                {
                    Debug.Log("Set English because of missing language");
                    LinkerSettings.Language = SystemLanguage.English;
                    return dic[SystemLanguage.English];
                }
            }
            return $"Missing: {id}";
        }

        [SerializeField] private TextAsset csv;
        private Dictionary<string, Dictionary<SystemLanguage, string>> localizationDic;
    }
}
