using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Control fast forward speed based on the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(DialogueBoxController))]
    public class FastForwardSpeedReader : MonoBehaviour
    {
        [SerializeField] private string configKeyName;

        private ConfigManager configManager;
        private DialogueBoxController dialogueBoxController;

        private void Awake()
        {
            configManager = Utils.FindNovaController().ConfigManager;
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
            // Convert speed to duration
            dialogueBoxController.fastForwardDelay = Mathf.Pow(0.1f, configManager.GetFloat(configKeyName));
        }
    }
}
