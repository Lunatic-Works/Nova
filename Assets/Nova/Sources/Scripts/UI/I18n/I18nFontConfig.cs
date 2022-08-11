using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class LocalizedFontConfig
    {
        public SystemLanguage locale;
        public TMP_FontAsset fontAsset;
        public SerializableDictionary<string, Material> materials;
    }

    public class I18nFontConfig : MonoBehaviour
    {
        private static I18nFontConfig Current;

        public static IEnumerable<LocalizedFontConfig> Config =>
            Current == null ? Enumerable.Empty<LocalizedFontConfig>() : Current.config;

        public List<LocalizedFontConfig> config;

        private void Awake()
        {
            Current = this;
        }

        private void OnDestroy()
        {
            Current = null;
        }
    }
}
