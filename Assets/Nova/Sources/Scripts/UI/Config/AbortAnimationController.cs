using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Toggle ability to abort animation based on the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(DialogueBoxController))]
    public class AbortAnimationController : MonoBehaviour
    {
        public string configKeyName;

        private ConfigManager configManager;
        private DialogueBoxController dialogueBoxController;

        private void Awake()
        {
            configManager = Utils.FindNovaGameController().ConfigManager;
            dialogueBoxController = GetComponent<DialogueBoxController>();
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
            dialogueBoxController.canAbortAnimation = configManager.GetInt(configKeyName) > 0;
        }
    }
}