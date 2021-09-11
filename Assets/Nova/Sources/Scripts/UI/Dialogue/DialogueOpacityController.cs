using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// Control opacity of dialogue box based on the value in ConfigManager
    /// </summary>
    public class DialogueOpacityController : MonoBehaviour
    {
        public string configKeyName;

        private CanvasGroup canvasGroup;
        private DialogueBoxController dialogueBoxController;
        private ConfigManager configManager;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            dialogueBoxController = GetComponent<DialogueBoxController>();
            this.RuntimeAssert(canvasGroup != null || dialogueBoxController != null,
                "Missing CanvasGroup or DialogueBoxController.");
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