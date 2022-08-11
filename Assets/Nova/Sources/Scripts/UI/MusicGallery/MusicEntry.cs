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
            if (displayNames.ContainsKey(I18n.CurrentLocale))
            {
                return displayNames[I18n.CurrentLocale];
            }
            else
            {
                return "(No title)";
            }
        }
    }
}
