using UnityEngine;

namespace Nova
{
    public class UnifiedAudioSource
    {
        public readonly AudioSource audioSource;
        public readonly AudioLooper audioLooper;

        public static implicit operator UnifiedAudioSource(AudioSource audioSource) =>
            new UnifiedAudioSource(audioSource);

        public static implicit operator UnifiedAudioSource(AudioLooper audioLooper) =>
            new UnifiedAudioSource(audioLooper);

        public UnifiedAudioSource(AudioSource audioSource)
        {
            this.audioSource = audioSource;
        }

        public UnifiedAudioSource(AudioLooper audioLooper)
        {
            this.audioLooper = audioLooper;
        }

        public AudioClip clip => audioLooper != null ? audioLooper.clip : audioSource.clip;

        public void SetClip(AudioClip clip, AudioClip headClip)
        {
            if (audioLooper != null) audioLooper.SetClip(clip, headClip);
            else audioSource.clip = clip;
        }

        public float volume
        {
            get => audioLooper != null ? audioLooper.volume : audioSource.volume;
            set
            {
                if (audioLooper != null) audioLooper.volume = value;
                else audioSource.volume = value;
            }
        }

        public int timeSamples => audioLooper != null ? audioLooper.timeSamples : audioSource.timeSamples;

        public bool isPlaying => audioLooper != null ? audioLooper.isPlaying : audioSource.isPlaying;

        public bool loop => audioLooper != null || audioSource.loop;

        public void Play()
        {
            if (audioLooper != null) audioLooper.Play();
            else audioSource.Play();
        }

        public void Stop()
        {
            if (audioLooper != null) audioLooper.Stop();
            else audioSource.Stop();
        }

        public void Pause()
        {
            if (audioLooper != null) audioLooper.Pause();
            else audioSource.Pause();
        }

        public void UnPause()
        {
            if (audioLooper != null) audioLooper.UnPause();
            else audioSource.UnPause();
        }
    }
}
