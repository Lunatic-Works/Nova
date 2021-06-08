using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class MusicEntry : ScriptableObject
    {
        public string id;
        public List<LocaleStringPair> displayNames;
        public string resourcePath;
        public int loopBeginSample;
        public int loopEndSample;

        public string GetDisplayName()
        {
            foreach (var pair in displayNames)
            {
                if (pair.locale == I18n.CurrentLocale)
                {
                    return pair.value;
                }
            }

            return "(No title)";
        }
    }
}