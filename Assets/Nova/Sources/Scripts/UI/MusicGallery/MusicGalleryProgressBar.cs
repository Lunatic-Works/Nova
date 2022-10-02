using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Slider))]
    public class MusicGalleryProgressBar : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
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

                // Add a nice 0.01s padding for seeking
                audioSource.time = Mathf.Min(audioSource.clip.length * Mathf.Clamp01(value),
                    audioSource.clip.length - 0.01f);
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

        private void RefreshTimeIndication()
        {
            if (audioSource == null || audioSource.clip == null)
            {
                timeLabel.text = FormatTimeLabel(0.0f, 0.0f);
                return;
            }

            timeLabel.text = FormatTimeLabel(audioSource.time, audioSource.clip.length);
        }

        private bool isDragging;

        private void Update()
        {
            if (!isDragging)
            {
                slider.SetValueWithoutNotify(progressRatio);
            }

            RefreshTimeIndication();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isDragging = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
            progressRatio = slider.value;
        }
    }
}
