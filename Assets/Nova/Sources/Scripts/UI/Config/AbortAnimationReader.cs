using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Toggle ability to abort animation based on the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(DialogueBoxController))]
    public class AbortAnimationReader : MonoBehaviour
    {
        [SerializeField] private string configKeyName;

        private ConfigManager configManager;
        private GameViewInput gameViewInput;

        private void Awake()
        {
            configManager = Utils.FindNovaController().ConfigManager;
            gameViewInput = GetComponent<GameViewInput>();
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
            gameViewInput.canAbortAnimation = configManager.GetInt(configKeyName) > 0;
        }
    }
}
