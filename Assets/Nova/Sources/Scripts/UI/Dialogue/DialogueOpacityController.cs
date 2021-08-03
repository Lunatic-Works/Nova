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

        private Image image;
        private DialogueBoxController dialogueBoxController;
        private ConfigManager configManager;

        private void Awake()
        {
            image = GetComponent<Image>();
            dialogueBoxController = GetComponent<DialogueBoxController>();
            this.RuntimeAssert(image != null || dialogueBoxController != null,
                "Missing Image or DialogueBoxController.");
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
                image.color = Utils.SetAlpha(image.color, configManager.GetFloat(configKeyName));
            }
        }
    }
}