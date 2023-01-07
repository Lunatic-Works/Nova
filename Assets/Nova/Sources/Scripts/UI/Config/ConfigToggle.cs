using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// Use toggle to modify the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class ConfigToggle : MonoBehaviour
    {
        [SerializeField] private string configKeyName;

        private Toggle toggle;
        private ConfigManager configManager;

        private void Awake()
        {
            this.RuntimeAssert(!string.IsNullOrEmpty(configKeyName), "Empty configKeyName.");

            toggle = GetComponent<Toggle>();
            configManager = Utils.FindNovaController().ConfigManager;
        }

        private void UpdateValue()
        {
            var value = configManager.GetInt(configKeyName) > 0;
            if (toggle.isOn == value)
            {
                // Eliminate infinite recursion
                return;
            }

            toggle.isOn = value;
        }

        private void OnValueChange(bool value)
        {
            configManager.SetInt(configKeyName, toggle.isOn ? 1 : 0);
        }

        private void OnEnable()
        {
            toggle.onValueChanged.AddListener(OnValueChange);
            configManager.AddValueChangeListener(configKeyName, UpdateValue);
            UpdateValue();
        }

        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(OnValueChange);
            configManager.RemoveValueChangeListener(configKeyName, UpdateValue);
        }
    }
}
