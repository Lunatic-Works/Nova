using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Toggle whether only fast forward read text based on the value in ConfigManager
    /// </summary>
    public class OnlyFastForwardReadController : MonoBehaviour
    {
        public string configKeyName;

        private DialogueState dialogueState;
        private ConfigManager configManager;

        private void Awake()
        {
            var controller = Utils.FindNovaGameController();
            dialogueState = controller.DialogueState;
            configManager = controller.ConfigManager;
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
            dialogueState.onlyFastForwardRead = configManager.GetInt(configKeyName) > 0;
        }
    }
}