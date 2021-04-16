using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class MusicGalleryVolumeController : MonoBehaviour
    {
        private Slider slider;
        private AudioSource audioSource;
        private ConfigManager config;

        public string configKey = "nova.music_gallery.volume";
        public float defaultVolume = 0.5f;

        private void LoadConfig()
        {
            var v = config.GetFloat(configKey, defaultVolume);
            audioSource.volume = v;
            slider.value = v;
        }

        private void Awake()
        {
            slider = GetComponent<Slider>();
            audioSource = GetComponentInParent<MusicGalleryPlayer>().audioSource;
            config = Utils.FindNovaGameController().ConfigManager;
            slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void OnDisable()
        {
            config.SetFloat(configKey, Mathf.Clamp01(slider.value));
            config.Apply();
        }

        private void OnValueChanged(float value)
        {
            audioSource.volume = value;
        }
    }
}