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

        public float pitch
        {
            get => audioSource.pitch;
            set
            {
                audioSource.pitch = value;
                headAudioSource.pitch = value;
            }
        }

        public int timeSamples
        {
            get
            {
                if (headAudioSource.isPlaying)
                {
                    return headAudioSource.timeSamples;
                }
                else
                {
                    return (headAudioSource.clip?.samples ?? 0) + audioSource.timeSamples;
                }
            }
            set
            {
                var headClip = headAudioSource.clip;
                if (headClip == null)
                {
                    audioSource.timeSamples = value;
                }
                else
                {
                    bool oldIsPlaying = isPlaying;
                    if (value < headClip.samples)
                    {
                        headAudioSource.timeSamples = value;
                        if (oldIsPlaying)
                        {
                            headAudioSource.UnPause();
                            PlayScheduledBody();
                        }
                    }
                    else
                    {
                        audioSource.timeSamples = value - headClip.samples;
                        headAudioSource.Stop();
                        if (oldIsPlaying)
                        {
                            audioSource.UnPause();
                        }
                    }
                }
            }
        }

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
            if (headAudioSource.clip != null)
            {
                headAudioSource.Play();
                PlayScheduledBody();
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
                headAudioSource.UnPause();
                PlayScheduledBody();
            }
            else
            {
                audioSource.UnPause();
            }
        }

        private void PlayScheduledBody()
        {
            AudioClip headClip = headAudioSource.clip;
            double timeLeft = (double)(headClip.samples - headAudioSource.timeSamples) / headClip.frequency;
            audioSource.PlayScheduled(AudioSettings.dspTime + timeLeft);
        }
    }
}
