using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Control fast forward speed based on the value in ConfigManager
    /// </summary>
    [RequireComponent(typeof(GameViewController))]
    public class FastForwardSpeedReader : MonoBehaviour
    {
        [SerializeField] private string configKeyName;

        private ConfigManager configManager;
        private GameViewController gameViewController;

        private void Awake()
        {
            configManager = Utils.FindNovaController().ConfigManager;
            gameViewController = GetComponent<GameViewController>();
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
            gameViewController.fastForwardDelay = Mathf.Pow(0.1f, configManager.GetFloat(configKeyName));
        }
    }
}
