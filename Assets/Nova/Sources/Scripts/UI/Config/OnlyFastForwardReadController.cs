using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Toggle whether only fast forward read text based on the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(DialogueBoxController))]
    public class OnlyFastForwardReadController : MonoBehaviour
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
            controller.onlyFastForwardRead = configManager.GetInt(configKeyName) > 0;
        }
    }
}