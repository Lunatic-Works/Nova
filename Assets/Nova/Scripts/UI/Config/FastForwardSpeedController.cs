using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Control fast forward speed based on the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(DialogueBoxController))]
    public class FastForwardSpeedController : MonoBehaviour
    {
        public string configKeyName;

        private DialogueBoxController dialogueBoxController;
        private ConfigManager configManager;

        private void Awake()
        {
            dialogueBoxController = GetComponent<DialogueBoxController>();
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
            dialogueBoxController.fastForwardDelay = Mathf.Pow(0.1f, configManager.GetFloat(configKeyName));
        }
    }
}