using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// Control font size based on the value in ConfigManager
    /// </summary>
    public class FontSizeReader : MonoBehaviour
    {
        [SerializeField] private string configKeyName;
        [SerializeField] private List<LocaleFloatPair> multipliers;

        private ConfigManager configManager;
        private Text text;
        private TMP_Text textPro;
        private TextProxy textProxy;

        private void Awake()
        {
            configManager = Utils.FindNovaController().ConfigManager;
            text = GetComponent<Text>();
            textPro = GetComponent<TMP_Text>();
            textProxy = GetComponent<TextProxy>();
            this.RuntimeAssert(text != null || textPro != null || textProxy != null,
                "Missing Text or TMP_Text or TextProxy.");
        }

        private void OnEnable()
        {
            configManager.AddValueChangeListener(configKeyName, UpdateValue);
            I18n.LocaleChanged.AddListener(UpdateValue);
            UpdateValue();
        }

        private void OnDisable()
        {
            configManager.RemoveValueChangeListener(configKeyName, UpdateValue);
            I18n.LocaleChanged.RemoveListener(UpdateValue);
        }

        private void UpdateValue()
        {
            float fontSize = configManager.GetFloat(configKeyName);
            foreach (var pair in multipliers)
            {
                if (pair.locale == I18n.CurrentLocale)
                {
                    fontSize *= pair.value;
                    break;
                }
            }

            if (textProxy != null)
            {
                textProxy.fontSize = fontSize;
            }
            else if (textPro != null)
            {
                textPro.fontSize = fontSize;
            }
            else
            {
                text.fontSize = Mathf.RoundToInt(fontSize);
            }
        }
    }
}
