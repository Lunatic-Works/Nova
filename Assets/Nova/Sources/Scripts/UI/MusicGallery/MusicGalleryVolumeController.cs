using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class MusicGalleryVolumeController : MonoBehaviour
    {
        private Slider slider;
        private AudioSource audioSource;
        private ConfigManager configManager;

        public string configKey = "nova.music_gallery.volume";
        public float defaultVolume = 0.5f;

        private void LoadConfig()
        {
            var v = configManager.GetFloat(configKey, defaultVolume);
            audioSource.volume = v;
            slider.value = v;
        }

        private void Awake()
        {
            slider = GetComponent<Slider>();
            audioSource = GetComponentInParent<MusicGalleryPlayer>().audioSource;
            configManager = Utils.FindNovaController().ConfigManager;
            slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDestroy()
        {
            slider.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void OnDisable()
        {
            configManager.SetFloat(configKey, Mathf.Clamp01(slider.value));
            configManager.Apply();
        }

        private void OnValueChanged(float value)
        {
            audioSource.volume = value;
        }
    }
}
