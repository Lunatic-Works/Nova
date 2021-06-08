using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Text))]
    public class I18nFontSize : MonoBehaviour
    {
        public List<LocaleFloatPair> multipliers;

        private Text text;
        private int fontSize;

        private void Awake()
        {
            text = GetComponent<Text>();
            fontSize = text.fontSize;
        }

        private void UpdateFontSize()
        {
            foreach (var pair in multipliers)
            {
                if (pair.locale == I18n.CurrentLocale)
                {
                    text.fontSize = Mathf.RoundToInt(fontSize * pair.value);
                    return;
                }
            }

            text.fontSize = fontSize;
        }

        private void OnEnable()
        {
            UpdateFontSize();
            I18n.LocaleChanged.AddListener(UpdateFontSize);
        }

        private void OnDisable()
        {
            I18n.LocaleChanged.RemoveListener(UpdateFontSize);
        }
    }
}