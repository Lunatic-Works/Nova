using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Toggle ability to skip read only based on the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(DialogueBoxController))]
    public class OnlySkipReadController : MonoBehaviour
    {
        public string configKeyName;

        private ConfigManager configManager;
        private DialogueBoxController controller;

        private void Awake()
        {
            configManager = Utils.FindNovaGameController().ConfigManager;
            controller = GetComponent<DialogueBoxController>();
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
            controller.onlySkipRead = configManager.GetInt(configKeyName) > 0;
        }
    }
}