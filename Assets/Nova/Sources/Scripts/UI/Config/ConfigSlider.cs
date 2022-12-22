using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// Use slider to modify the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class ConfigSlider : MonoBehaviour
    {
        [SerializeField] private string configKeyName;

        private Slider slider;
        private ConfigManager configManager;

        private void Awake()
        {
            this.RuntimeAssert(!string.IsNullOrEmpty(configKeyName), "Empty configKeyName.");

            slider = GetComponent<Slider>();
            configManager = Utils.FindNovaController().ConfigManager;
            var def = configManager.GetDefinition(configKeyName);
            this.RuntimeAssert(def != null, $"Definition not found for config key {configKeyName}.");
            slider.maxValue = def.max ?? 1;
            slider.minValue = def.min ?? 0;
            slider.wholeNumbers = def.whole == true;
        }

        private void UpdateValue()
        {
            var value = configManager.GetFloat(configKeyName);
            if (Mathf.Approximately(value, slider.value))
            {
                // Eliminate infinite recursion
                return;
            }

            slider.value = value;
        }

        private void OnValueChange(float value)
        {
            configManager.SetFloat(configKeyName, value);
        }

        private void OnEnable()
        {
            slider.onValueChanged.AddListener(OnValueChange);
            configManager.AddValueChangeListener(configKeyName, UpdateValue);
            UpdateValue();
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(OnValueChange);
            configManager.RemoveValueChangeListener(configKeyName, UpdateValue);
        }
    }
}
