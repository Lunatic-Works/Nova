using UnityEngine;

namespace Nova
{
    public class TextSpeedReader : MonoBehaviour
    {
        [SerializeField] private string configKeyName;

        private ConfigManager configManager;
        private DialogueBoxController dialogueBoxController;
        private ConfigTextPreviewController configTextPreviewController;

        private void Awake()
        {
            configManager = Utils.FindNovaController().ConfigManager;
            dialogueBoxController = GetComponent<DialogueBoxController>();
            configTextPreviewController = GetComponent<ConfigTextPreviewController>();
            this.RuntimeAssert(
                dialogueBoxController != null || configTextPreviewController != null,
                "Missing DialogueBoxController or ConfigTextPreviewController."
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
            float val = Mathf.Max(2f * Mathf.Pow(0.1f, configManager.GetFloat(configKeyName)) - 0.02f, 0.001f);
            if (dialogueBoxController != null)
            {
                dialogueBoxController.characterFadeInDuration = val;
            }
            else // configTextPreviewController != null
            {
                configTextPreviewController.characterFadeInDuration = val;
            }
        }
    }
}
