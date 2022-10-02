using UnityEngine;

namespace Nova
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioLooper : MonoBehaviour
    {
        private AudioSource audioSource;
        private AudioSource headAudioSource;
        private bool headPaused;

        public AudioClip clip => audioSource.clip;

        public float volume
        {
            get => audioSource.volume;
            set
            {
                audioSource.volume = value;
                headAudioSource.volume = value;
            }
        }

        public int timeSamples => audioSource.timeSamples;

        public bool isPlaying => audioSource.isPlaying || headAudioSource.isPlaying;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();

            var head = new GameObject("AudioLooperHead");
            head.transform.SetParent(transform, false);
            headAudioSource = head.AddComponent<AudioSource>();
            headAudioSource.playOnAwake = false;
            headAudioSource.loop = false;
        }

        public void SetClip(AudioClip clip, AudioClip headClip)
        {
            audioSource.clip = clip;
            headAudioSource.clip = headClip;
        }

        public void Play()
        {
            AudioClip headClip = headAudioSource.clip;
            if (headClip != null)
            {
                headAudioSource.Play();
                double timeLeft = (double)headClip.samples / headClip.frequency;
                audioSource.PlayScheduled(AudioSettings.dspTime + timeLeft);
            }
            else
            {
                audioSource.Play();
            }
        }

        public void Stop()
        {
            headAudioSource.Stop();
            audioSource.Stop();
        }

        public void Pause()
        {
            if (headAudioSource.isPlaying)
            {
                headAudioSource.Pause();
                audioSource.Stop();
                headPaused = true;
            }
            else
            {
                audioSource.Pause();
                headPaused = false;
            }
        }

        public void UnPause()
        {
            if (headPaused)
            {
                AudioClip headClip = headAudioSource.clip;
                headAudioSource.UnPause();
                double timeLeft = (double)(headClip.samples - headAudioSource.timeSamples) / headClip.frequency;
                audioSource.PlayScheduled(AudioSettings.dspTime + timeLeft);
            }
            else
            {
                audioSource.UnPause();
            }
        }
    }
}
