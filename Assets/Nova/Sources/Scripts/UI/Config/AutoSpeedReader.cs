using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Control auto mode speed based on the value in ConfigManager
    /// </summary>
    public class AutoSpeedReader : MonoBehaviour
    {
        [SerializeField] private string configKeyName;

        private ConfigManager configManager;
        private GameViewController gameViewController;
        private ConfigTextPreviewController configTextPreviewController;

        private void Awake()
        {
            configManager = Utils.FindNovaController().ConfigManager;
            gameViewController = GetComponent<GameViewController>();
            configTextPreviewController = GetComponent<ConfigTextPreviewController>();
            this.RuntimeAssert(
                gameViewController != null || configTextPreviewController != null,
                "Missing GameViewController or ConfigTextPreviewController."
            );
        }

        private void OnEnable()
        {
            configManager.AddValueChangeListener(configKeyName, UpdateValue);
            UpdateValue();
        }

        private void OnDisable()
        {
            configManager.RemoveValueChangeListener(configKeyName, UpdateValue);
        }

        private void UpdateValue()
        {
            // Convert speed to duration
            float val = 10f * Mathf.Pow(0.2f, configManager.GetFloat(configKeyName));
            if (gameViewController != null)
            {
                gameViewController.autoDelay = val;
            }
            else
            {
                configTextPreviewController.autoDelay = val;
            }
        }
    }
}
