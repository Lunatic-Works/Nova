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

        private ConfigManager configManager;
        private CanvasGroup canvasGroup;
        private DialogueBoxController dialogueBoxController;

        private void Awake()
        {
            configManager = Utils.FindNovaGameController().ConfigManager;
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