using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Control opacity of dialogue box based on the value in ConfigManager
    /// </summary>
    public class DialogueOpacityReader : MonoBehaviour
    {
        [SerializeField] private string configKeyName;

        private ConfigManager configManager;
        private CanvasGroup canvasGroup;
        private DialogueBoxController dialogueBoxController;

        private void Awake()
        {
            configManager = Utils.FindNovaController().ConfigManager;
            canvasGroup = GetComponent<CanvasGroup>();
            dialogueBoxController = GetComponent<DialogueBoxController>();
            this.RuntimeAssert(canvasGroup != null || dialogueBoxController != null,
                "Missing CanvasGroup or DialogueBoxController.");
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
            if (dialogueBoxController != null)
            {
                dialogueBoxController.configOpacity = configManager.GetFloat(configKeyName);
            }
            else
            {
                canvasGroup.alpha = configManager.GetFloat(configKeyName);
            }
        }
    }
}
