using UnityEngine;

namespace Nova
{
    public class MusicEntry : ScriptableObject
    {
        public string id;
        public SerializableDictionary<SystemLanguage, string> displayNames;
        public string resourcePath;
        public int loopBeginSample;
        public int loopEndSample;

        public string GetDisplayName()
        {
            if (displayNames.TryGetValue(I18n.CurrentLocale, out var name))
            {
                return name;
            }
            else
            {
                return "(No title)";
            }
        }
    }
}
