using UnityEngine;

namespace Nova
{
    public class LanguageController : MonoBehaviour
    {
        public string configKeyName;
        public LocaleTogglePair[] localeTogglePairs;

        private ConfigManager configManager;

        private void Awake()
        {
            configManager = Utils.FindNovaGameController().ConfigManager;
            var localeInt = configManager.GetInt(configKeyName);
            if (localeInt < 0)
            {
                configManager.SetInt(configKeyName, (int) I18n.CurrentLocale);
            }
            else
            {
                I18n.CurrentLocale = (SystemLanguage) localeInt;
            }

            this.RuntimeAssert(localeTogglePairs.Length > 0, "Empty language toggle list");
            foreach (var pair in localeTogglePairs)
            {
                pair.toggle.isOn = pair.locale == I18n.CurrentLocale;
            }

            foreach (var pair in localeTogglePairs)
            {
                pair.toggle.onValueChanged.AddListener(value => SetLocale(value, pair.locale));
            }
        }

        private void SetLocale(bool value, SystemLanguage locale)
        {
            if (!value) return;
            I18n.CurrentLocale = locale;
            configManager.SetInt(configKeyName, (int) locale);
        }
    }
}