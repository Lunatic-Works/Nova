using System;
using UnityEngine;

namespace Nova
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(AudioSource))]
    public class AudioLooperOld : MonoBehaviour
    {
        private AudioSource[] audioSources;
        private int currentAudioSourceIndex = 0;
        private GameObject ghost;
        private double dspTimeRealTimeDelta;

        public AudioSource currentAudioSource => audioSources[currentAudioSourceIndex];

        public float volume
        {
            get => currentAudioSource.volume;
            set
            {
                foreach (var audioSource in audioSources)
                    audioSource.volume = value;
            }
        }

        public AudioClip clip
        {
            get => currentAudioSource.clip;
            set
            {
                if (clip == value)
                    return;
                Stop();
                foreach (var audioSource in audioSources)
                    audioSource.clip = value;
            }
        }

        public MusicEntry musicEntry;

        public bool isPlaying => enabled;

        public static double CurrentMillis => (double)DateTime.Now.Ticks / TimeSpan.TicksPerSecond;

        private void Awake()
        {
            ghost = new GameObject("AudioLooperGhost");
            ghost.transform.SetParent(transform, false);
            audioSources = new[]
            {
                GetComponent<AudioSource>(),
                ghost.AddComponent<AudioSource>()
            };
            foreach (var audioSource in audioSources)
                audioSource.loop = false;
            enabled = false;
            dspTimeRealTimeDelta = AudioSettings.dspTime - CurrentMillis;
        }

        private void Schedule()
        {
            int loopEnd = Math.Min(musicEntry.loopEndSample, clip.samples);
            double alternateTime = dspTimeRealTimeDelta + CurrentMillis +
                                   1.0 * (loopEnd - currentAudioSource.timeSamples) / clip.frequency;
            currentAudioSource.SetScheduledEndTime(alternateTime);
            var other = audioSources[1 - currentAudioSourceIndex];
            other.timeSamples = musicEntry.loopBeginSample;
            other.PlayScheduled(alternateTime);
        }

        private void Validation()
        {
            this.RuntimeAssert(clip != null, "Missing clip when playing loopable music.");
            this.RuntimeAssert(musicEntry != null, "Missing musicEntry when playing loopable music.");
            // this.RuntimeAssert(
            //     musicEntry.loopEndSample <= clip.samples,
            //     $"Loop end {musicEntry.loopEndSample} is larger than length {clip.samples}."
            // );
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            dspTimeRealTimeDelta = AudioSettings.dspTime - CurrentMillis;
        }

        public void Play()
        {
            if (currentAudioSource.isPlaying)
                return;
            Validation();
            enabled = true;
            currentAudioSource.timeSamples = 0;
            currentAudioSource.Play();
            Schedule();
        }

        public void Stop()
        {
            enabled = false;
            foreach (var audioSource in audioSources)
                audioSource.Stop(); // Including scheduled play
        }

        public void Pause()
        {
            enabled = false;
            foreach (var audioSource in audioSources)
                audioSource.Pause();
        }

        public void UnPause()
        {
            if (currentAudioSource.isPlaying)
                return;
            enabled = true;
            foreach (var audioSource in audioSources)
                audioSource.UnPause();
            Schedule();
        }

        public void Update()
        {
            if (!currentAudioSource.isPlaying)
            {
                currentAudioSourceIndex = 1 - currentAudioSourceIndex;
                Schedule();
            }
        }

        public void SetProgress(int toSamples)
        {
            enabled = true;
            currentAudioSource.timeSamples = toSamples;
            currentAudioSource.Play();
            Schedule();
        }
    }
}
