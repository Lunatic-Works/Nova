using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Control whether to stop voice on dialogue change based on the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class StopVoiceOnDialogueChangeController : MonoBehaviour
    {
        public string configKeyName;

        private CharacterController characterController;
        private ConfigManager configManager;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
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
            characterController.stopVoiceWhenDialogueWillChange = configManager.GetInt(configKeyName) > 0;
        }
    }
}