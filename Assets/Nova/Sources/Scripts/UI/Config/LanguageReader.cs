using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class LanguageReader : MonoBehaviour
    {
        [SerializeField] private string configKeyName;
        [SerializeField] private SerializableDictionary<SystemLanguage, Toggle> localeTogglePairs;

        private ConfigManager configManager;

        private void Awake()
        {
            configManager = Utils.FindNovaController().ConfigManager;
            var localeInt = configManager.GetInt(configKeyName);
            if (localeInt >= 0)
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
        }

        private void OnLocaleChanged()
        {
            configManager.SetInt(configKeyName, (int)I18n.CurrentLocale);
            foreach (var pair in localeTogglePairs)
            {
                pair.Value.SetIsOnWithoutNotify(pair.Key == I18n.CurrentLocale);
            }
        }
    }
}
