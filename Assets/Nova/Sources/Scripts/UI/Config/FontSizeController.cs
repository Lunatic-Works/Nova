using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// Control font size based on the value in ConfigManager
    /// </summary>
    public class FontSizeController : MonoBehaviour
    {
        public string configKeyName;
        public SerializableDictionary<SystemLanguage, float> multipliers;

        private Text text;
        private TMP_Text textPro;
        private TextProxy textProxy;
        private ConfigManager configManager;

        private void Awake()
        {
            text = GetComponent<Text>();
            textPro = GetComponent<TMP_Text>();
            textProxy = GetComponent<TextProxy>();
            this.RuntimeAssert(text != null || textPro != null || textProxy != null,
                "Missing Text or TMP_Text or TextProxy.");
            configManager = Utils.FindNovaGameController().ConfigManager;
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
            if (multipliers.ContainsKey(I18n.CurrentLocale))
            {
                fontSize *= multipliers[I18n.CurrentLocale];
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
