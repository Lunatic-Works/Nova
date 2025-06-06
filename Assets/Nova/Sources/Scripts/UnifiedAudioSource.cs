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

        public void SetClip(AudioClip clip, MusicEntryTimes musicEntry)
        {
            if (audioLooper != null)
            {
                if (musicEntry == null)
                {
                    musicEntry = clip;
                }

                audioLooper.SetClip(clip, musicEntry);
            }
            else
            {
                audioSource.clip = clip;
                Utils.RuntimeAssert(musicEntry == null,
                    $"MusicEntry {musicEntry} should be null on AudioSource {Utils.GetPath(audioSource)}");
            }
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

        public float pitch
        {
            get => audioLooper != null ? audioLooper.pitch : audioSource.pitch;
            set
            {
                if (audioLooper != null) audioLooper.pitch = value;
                else audioSource.pitch = value;
            }
        }

        public int timeSamples
        {
            get => audioLooper != null ? audioLooper.timeSamples : audioSource.timeSamples;
            set
            {
                if (audioLooper != null) audioLooper.timeSamples = value;
                else audioSource.timeSamples = value;
            }
        }

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

        public override string ToString()
        {
            return audioLooper != null ? Utils.GetPath(audioLooper) : Utils.GetPath(audioSource);
        }
    }
}
