using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class LanguageController : MonoBehaviour
    {
        public string configKeyName;
        public SerializableDictionary<SystemLanguage, Toggle> localeTogglePairs;

        private ConfigManager configManager;

        private void Awake()
        {
            configManager = Utils.FindNovaGameController().ConfigManager;
            var localeInt = configManager.GetInt(configKeyName);
            if (localeInt < 0)
            {
                configManager.SetInt(configKeyName, (int)I18n.CurrentLocale);
            }
            else
            {
                I18n.CurrentLocale = (SystemLanguage)localeInt;
            }

            this.RuntimeAssert(localeTogglePairs.Count > 0, "Empty language toggle list.");
            foreach (var pair in localeTogglePairs)
            {
                pair.Value.onValueChanged.AddListener(value => SetLocale(value, pair.Key));
            }

            OnLocaleChanged();
            I18n.LocaleChanged.AddListener(OnLocaleChanged);
        }

        private void OnDestroy()
        {
            I18n.LocaleChanged.RemoveListener(OnLocaleChanged);
        }

        private void SetLocale(bool value, SystemLanguage locale)
        {
            if (!value) return;
            I18n.CurrentLocale = locale;
            configManager.SetInt(configKeyName, (int)locale);
        }

        private void OnLocaleChanged()
        {
            foreach (var pair in localeTogglePairs)
            {
                pair.Value.SetIsOnWithoutNotify(pair.Key == I18n.CurrentLocale);
            }
        }
    }
}
