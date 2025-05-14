using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Control audio volume based on the value in ConfigManager
    /// </summary>
    public class AudioVolumeReader : MonoBehaviour
    {
        private const string GlobalConfigKeyName = "GlobalVolume";

        [SerializeField] private string configKeyName;
        [SerializeField] private string secondaryConfigKeyName;
        [SerializeField] private float multiplier = 1.0f;
        [SerializeField] private float gamma = 1.0f;

        private ConfigManager configManager;
        private AudioController audioController;
        private SoundController soundController;
        private AudioSource audioSource;
        private VideoController videoController;

        private void Awake()
        {
            configManager = Utils.FindNovaController().ConfigManager;
            audioController = GetComponent<AudioController>();
            soundController = GetComponent<SoundController>();
            audioSource = GetComponent<AudioSource>();
            videoController = GetComponent<VideoController>();
            this.RuntimeAssert(
                audioController != null || soundController != null || audioSource != null || videoController != null,
                "Missing AudioController or SoundController or AudioSource or VideoController."
            );
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

        private void UpdateValue()
        {
            var globalValue = configManager.GetFloat(GlobalConfigKeyName);
            var masterValue = configManager.GetFloat(configKeyName);
            float secondaryValue;
            if (string.IsNullOrEmpty(secondaryConfigKeyName))
            {
                secondaryValue = 1.0f;
                masterValue = Mathf.Pow(masterValue, gamma);
            }
            else
            {
                secondaryValue = configManager.GetFloat(secondaryConfigKeyName);
                secondaryValue = Mathf.Pow(secondaryValue, gamma);
            }

            float value = multiplier * globalValue * masterValue * secondaryValue;

            if (audioController != null)
            {
                audioController.configVolume = value;
            }
            else if (soundController != null)
            {
                soundController.configVolume = value;
            }
            else if (audioSource != null)
            {
                audioSource.volume = Utils.LogToLinearVolume(value);
            }
            else
            {
                videoController.volume = Utils.LogToLinearVolume(value);
            }
        }
    }
}
