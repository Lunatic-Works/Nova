using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class FontMaterialConfig
    {
        public string name;
        public Material material;
    }

    [Serializable]
    public class LocalizedFontConfig
    {
        public SystemLanguage locale;
        public TMP_FontAsset fontAsset;
        public List<FontMaterialConfig> materials;
    }

    public class I18nFontConfig : MonoBehaviour
    {
        private static I18nFontConfig Current;

        public static IEnumerable<LocalizedFontConfig> Config => Current.config;

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