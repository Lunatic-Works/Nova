using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Toggle whether to fast forward unread text based on the value in ConfigManager
    /// </summary>
    public class FastForwardUnreadReader : MonoBehaviour
    {
        [SerializeField] private string configKeyName;

        private DialogueState dialogueState;
        private ConfigManager configManager;

        private void Awake()
        {
            var controller = Utils.FindNovaController();
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
            dialogueState.fastForwardUnread = configManager.GetInt(configKeyName) > 0;
        }
    }
}
