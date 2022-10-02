using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Control audio volume based on the value in ConfigManager
    /// </summary>
    public class AudioVolumeController : MonoBehaviour
    {
        private const string GlobalConfigKeyName = "GlobalVolume";

        public string configKeyName;
        public string secondaryConfigKeyName;
        public float multiplier = 1.0f;
        public float gamma = 1.0f;

        private AudioController audioController;
        private SoundController soundController;
        private AudioSource audioSource;
        private ConfigManager configManager;

        private void Awake()
        {
            audioController = GetComponent<AudioController>();
            soundController = GetComponent<SoundController>();
            audioSource = GetComponent<AudioSource>();
            this.RuntimeAssert(audioController != null || soundController != null || audioSource != null,
                "Missing AudioController or SoundController or AudioSource.");
            configManager = Utils.FindNovaGameController().ConfigManager;
        }

        private void OnEnable()
        {
            configManager.AddValueChangeListener(GlobalConfigKeyName, UpdateValue);
            configManager.AddValueChangeListener(configKeyName, UpdateValue);
            if (!string.IsNullOrEmpty(secondaryConfigKeyName))
            {
                configManager.AddValueChangeListener(secondaryConfigKeyName, UpdateValue);
            }

            UpdateValue();
        }

        private void OnDisable()
        {
            configManager.RemoveValueChangeListener(GlobalConfigKeyName, UpdateValue);
            configManager.RemoveValueChangeListener(configKeyName, UpdateValue);
            if (!string.IsNullOrEmpty(secondaryConfigKeyName))
            {
                configManager.RemoveValueChangeListener(secondaryConfigKeyName, UpdateValue);
            }
        }

        private float globalValue => configManager.GetFloat(GlobalConfigKeyName);

        private float masterValue => configManager.GetFloat(configKeyName);

        private float secondaryValue =>
            string.IsNullOrEmpty(secondaryConfigKeyName) ? 1.0f : configManager.GetFloat(secondaryConfigKeyName);

        private float combinedValue => globalValue * masterValue * secondaryValue;

        private void UpdateValue()
        {
            float value = multiplier * Mathf.Pow(combinedValue, gamma);
            if (audioController != null)
            {
                audioController.configVolume = value;
            }
            else if (soundController != null)
            {
                soundController.configVolume = value;
            }
            else
            {
                audioSource.volume = value;
            }
        }
    }
}
