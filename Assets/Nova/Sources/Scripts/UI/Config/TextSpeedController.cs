using UnityEngine;

namespace Nova
{
    public class TextSpeedController : MonoBehaviour
    {
        public string configKeyName = "TextSpeed";

        private DialogueBoxController dialogueBoxController;
        private ConfigTextPreviewController configTextPreviewController;
        private ConfigManager configManager;

        private void Awake()
        {
            dialogueBoxController = GetComponent<DialogueBoxController>();
            configTextPreviewController = GetComponent<ConfigTextPreviewController>();
            this.RuntimeAssert(
                dialogueBoxController != null || configTextPreviewController != null,
                "Missing DialogueBoxController or ConfigTextPreviewController."
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
            float val = Mathf.Max(2f * Mathf.Pow(0.1f, configManager.GetFloat(configKeyName)) - 0.02f, 0.001f);
            if (dialogueBoxController != null)
            {
                dialogueBoxController.perCharacterFadeInDuration = val;
            }
            else
            {
                configTextPreviewController.perCharacterFadeInDuration = val;
            }
        }
    }
}