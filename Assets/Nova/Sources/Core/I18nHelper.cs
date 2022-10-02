// Those classes are only used to edit list of pairs in Unity Editor

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [Serializable]
    public class LocaleFloatPair
    {
        public SystemLanguage locale;
        public float value;
    }

    [Serializable]
    public class LocaleStringPair
    {
        public SystemLanguage locale;
        public string value;
    }

    [Serializable]
    public class LocaleSpritePair
    {
        public SystemLanguage locale;
        public Sprite sprite;
    }

    [Serializable]
    public class LocaleTogglePair
    {
        public SystemLanguage locale;
        public Toggle toggle;
    }
}
