using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Control whether to stop voice on dialogue change based on the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(GameCharacterController))]
    public class StopVoiceOnDialogueChangeController : MonoBehaviour
    {
        public string configKeyName;

        private GameCharacterController characterController;
        private ConfigManager configManager;

        private void Awake()
        {
            characterController = GetComponent<GameCharacterController>();
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
            characterController.stopVoiceOnDialogueChange = configManager.GetInt(configKeyName) > 0;
        }
    }
}
