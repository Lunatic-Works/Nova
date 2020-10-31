using System;
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
        public FontMaterialConfig[] materials;
    }

    public class I18nFontConfig : MonoBehaviour
    {
        private static I18nFontConfig Current;

        public static LocalizedFontConfig[] Config => Current.config;

        public LocalizedFontConfig[] config;

        private void Awake()
        {
            Current = this;
        }
    }
}