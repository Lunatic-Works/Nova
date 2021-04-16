using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Control auto mode speed based on the value in ConfigManager
    /// </summary>
    public class AutoSpeedController : MonoBehaviour
    {
        public string configKeyName;

        private DialogueBoxController dialogueBoxController;
        private ConfigTextPreviewController configTextPreviewController;
        private ConfigManager configManager;

        private void Awake()
        {
            dialogueBoxController = GetComponent<DialogueBoxController>();
            configTextPreviewController = GetComponent<ConfigTextPreviewController>();
            this.RuntimeAssert(
                dialogueBoxController != null || configTextPreviewController != null,
                "Missing DialogueBoxController or ConfigViewController."
            );
            configManager = Utils.FindNovaGameController().ConfigManager;
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
            float val = 10.0f * Mathf.Pow(0.2f, configManager.GetFloat(configKeyName));
            if (dialogueBoxController != null)
                dialogueBoxController.autoDelay = val;
            else
                configTextPreviewController.autoDelay = val;
        }
    }
}