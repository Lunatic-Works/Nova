using System;
using UnityEngine;

namespace Nova
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(AudioSource))]
    public class AudioLooper : MonoBehaviour
    {
        private GameObject ghost;
        private AudioSource[] audioSources;
        [ReadOnly] [SerializeField] private int currentAudioSourceIndex = 0;

        private MusicEntryTimes musicEntry;
        // For debug
        [ReadOnly] [SerializeField] private int loopBeginSample;
        [ReadOnly] [SerializeField] private int loopEndSample;

        private AudioSource currentAudioSource => audioSources[currentAudioSourceIndex];

        public AudioClip clip => currentAudioSource.clip;

        public float volume
        {
            get => currentAudioSource.volume;
            set
            {
                foreach (var audioSource in audioSources)
                    audioSource.volume = value;
            }
        }

        public float pitch
        {
            get => currentAudioSource.pitch;
            set
            {
                foreach (var audioSource in audioSources)
                    audioSource.pitch = value;
            }
        }

        public int timeSamples
        {
            get => currentAudioSource.timeSamples;
            set => currentAudioSource.timeSamples = value;
        }

        public bool isPlaying => enabled;

        private void Awake()
        {
            var _audioSource = GetComponent<AudioSource>();

            ghost = new GameObject("AudioLooperGhost");
            ghost.transform.SetParent(transform, false);
            var ghostAudioSource = ghost.AddComponent<AudioSource>();
            ghostAudioSource.outputAudioMixerGroup = _audioSource.outputAudioMixerGroup;

            audioSources = new[] {_audioSource, ghostAudioSource};
            foreach (var audioSource in audioSources)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }

            enabled = false;
        }

        public void SetClip(AudioClip clip, MusicEntryTimes musicEntry)
        {
            Stop();
            foreach (var audioSource in audioSources)
                audioSource.clip = clip;
            this.musicEntry = musicEntry;

            if (musicEntry == null)
            {
                this.loopBeginSample = -1;
                this.loopEndSample = -1;
            }
            else
            {
                this.loopBeginSample = musicEntry.loopBeginSample;
                this.loopEndSample = musicEntry.loopEndSample;
            }
        }

        private void Schedule()
        {
            int loopEnd = Math.Min(musicEntry.loopEndSample, clip.samples);
            double alternateTime = AudioSettings.dspTime +
                                   (double)(loopEnd - currentAudioSource.timeSamples) / clip.frequency;
            currentAudioSource.SetScheduledEndTime(alternateTime);

            var other = audioSources[1 - currentAudioSourceIndex];
            other.timeSamples = musicEntry.loopBeginSample;
            other.PlayScheduled(alternateTime);
        }

        private void Validate()
        {
            this.RuntimeAssert(clip != null, "Missing clip when playing loopable music.");
            this.RuntimeAssert(musicEntry != null, "Missing musicEntry when playing loopable music.");
            // this.RuntimeAssert(
            //     musicEntry.loopEndSample <= clip.samples,
            //     $"Loop end {musicEntry.loopEndSample} is larger than length {clip.samples}."
            // );
        }

        public void Play()
        {
            if (currentAudioSource.isPlaying)
                return;
            Validate();
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
            foreach (var audioSource in audioSources)
                audioSource.UnPause();
            if (currentAudioSource.isPlaying)
            {
                enabled = true;
                Schedule();
            }
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
