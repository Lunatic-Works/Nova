using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class I18nHelper
    {
        // internal name -> locale -> display name
        private readonly Dictionary<string, Dictionary<SystemLanguage, string>> map =
            new Dictionary<string, Dictionary<SystemLanguage, string>>();

        public string Get(string internalName)
        {
            return I18n.__(map[internalName]);
        }

        public void Set(string internalName, SystemLanguage locale, string displayName)
        {
            if (!map.ContainsKey(internalName))
            {
                map[internalName] = new Dictionary<SystemLanguage, string>();
            }

            map[internalName][locale] = displayName;
        }
    }

    // Used in editor
    [Serializable]
    public class LocaleStringPair
    {
        public SystemLanguage locale;
        public string value;
    }

    [Serializable]
    public class LocaleTogglePair
    {
        public SystemLanguage locale;
        public Toggle toggle;
    }
}
