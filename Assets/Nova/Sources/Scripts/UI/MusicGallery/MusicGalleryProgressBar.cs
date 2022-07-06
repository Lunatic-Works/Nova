using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class MusicGalleryProgressBar : MonoBehaviour
    {
        public Text timeLabel;

        private MusicGalleryPlayer player;
        private Slider slider;

        public bool interactable
        {
            get => slider.interactable;
            set => slider.interactable = value;
        }

        private AudioSource audioSource => player.audioSource;

        private bool inited;

        public void Init()
        {
            if (inited)
            {
                return;
            }

            player = GetComponentInParent<MusicGalleryPlayer>();
            slider = GetComponent<Slider>();
            slider.value = 0.0f;
            RefreshTimeIndication();
            inited = true;
        }

        private void Awake()
        {
            Init();
            slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDestroy()
        {
            slider.onValueChanged.RemoveListener(OnValueChanged);
        }

        private float progressRatio
        {
            get
            {
                if (audioSource == null || audioSource.clip == null)
                {
                    return 0.0f;
                }

                return audioSource.time / audioSource.clip.length;
            }
            set
            {
                if (audioSource == null || audioSource.clip == null)
                {
                    return;
                }

                // add nice 0.01s padding for seeking
                audioSource.time = Mathf.Min(audioSource.clip.length * Mathf.Clamp01(value), audioSource.clip.length - 0.01f);
            }
        }

        private static void GetMinuteAndSecond(float time, out int minute, out int second)
        {
            var t = Mathf.RoundToInt(time);
            minute = t / 60;
            second = t % 60;
        }

        private static string FormatTimeLabel(float time, float total)
        {
            GetMinuteAndSecond(time, out var m, out var s);
            GetMinuteAndSecond(total, out var tm, out var ts);
            return $"{m:D2}:{s:D2}/{tm:D2}:{ts:D2}";
        }

        private bool audioShouldPlay = false;

        private void Pause()
        {
            audioShouldPlay = player.isPlaying;
            player.Pause();
        }

        private void Resume()
        {
            if (audioShouldPlay)
            {
                player.Play();
            }
        }

        private bool _isDragged = false;

        public bool isDragged
        {
            get => _isDragged;
            set
            {
                if (_isDragged == value)
                {
                    return;
                }

                _isDragged = value;
                if (_isDragged)
                {
                    Pause();
                }
                else
                {
                    Resume();
                }
            }
        }

        private void RefreshTimeIndication()
        {
            if (audioSource == null || audioSource.clip == null)
            {
                timeLabel.text = FormatTimeLabel(0.0f, 0.0f);
                return;
            }

            timeLabel.text = FormatTimeLabel(audioSource.time, audioSource.clip.length);
        }

        private void OnValueChanged(float value)
        {
            if (isDragged)
            {
                progressRatio = value;
            }
        }

        private void Update()
        {
            if (!isDragged)
            {
                slider.value = progressRatio;
            }

            RefreshTimeIndication();
        }
    }
}