using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Control whether to stop voice on dialogue change based on the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(GameCharacterController))]
    public class StopVoiceOnDialogueChangeReader : MonoBehaviour
    {
        [SerializeField] private string configKeyName;

        private ConfigManager configManager;
        private GameCharacterController characterController;

        private void Awake()
        {
            configManager = Utils.FindNovaController().ConfigManager;
            characterController = GetComponent<GameCharacterController>();
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
