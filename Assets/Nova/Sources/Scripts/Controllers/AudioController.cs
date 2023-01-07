using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// This class is used for controlling audio source from external scripts
    /// </summary>
    [ExportCustomType]
    [RequireComponent(typeof(AudioSource))]
    public class AudioController : MonoBehaviour, IRestorable
    {
        public string luaGlobalName;
        public string audioFolder;

        [SerializeField] private MusicEntryList musicEntryList;

        // For debug
        [SerializeField] private Slider slider;
        [SerializeField] private Text text;

        public string currentAudioName { get; private set; }
        private string lastAudioName;

        public bool isPlaying { get; private set; }
        private bool lastIsPlaying;

        private GameState gameState;
        private UnifiedAudioSource audioSource;

        private float _scriptVolume;

        public float scriptVolume
        {
            get => _scriptVolume;
            set
            {
                _scriptVolume = value;
                Init();
                audioSource.volume = _scriptVolume * _configVolume;
            }
        }

        private float _configVolume;

        public float configVolume
        {
            get => _configVolume;
            set
            {
                _configVolume = value;
                Init();
                audioSource.volume = _scriptVolume * _configVolume;
            }
        }

        private bool inited;

        private void Init()
        {
            if (inited)
            {
                return;
            }

            gameState = Utils.FindNovaController().GameState;

            if (musicEntryList != null)
            {
                audioSource = gameObject.AddComponent<AudioLooper>();
            }
            else
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                LuaRuntime.Instance.BindObject(luaGlobalName, this, "_G");
                gameState.AddRestorable(this);
            }

            inited = true;
        }

        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(luaGlobalName))
            {
                gameState.RemoveRestorable(this);
            }
        }

        // Do not call ForceUpdate() frequently, because change of BGM needs to be smooth
        private void Update()
        {
            if (audioSource.clip != null)
            {
                if (slider != null)
                {
                    slider.value = 1.0f * audioSource.timeSamples / audioSource.clip.samples;
                }

                if (text != null)
                {
                    text.text = $"{audioSource.timeSamples} / {audioSource.clip.samples}";
                }
            }

            if (currentAudioName == lastAudioName && isPlaying == lastIsPlaying) return;
            ForceUpdate();
        }

        private void ForceUpdate()
        {
            AudioClip clip = null;
            AudioClip headClip = null;

            if (!string.IsNullOrEmpty(currentAudioName))
            {
                var audioPath = System.IO.Path.Combine(audioFolder, currentAudioName);
                clip = AssetLoader.LoadOrNull<AudioClip>(audioPath + "_loop");
                if (clip != null)
                {
                    // To reduce the possibility of discontinuity between head and loop,
                    // we use the full audio as head
                    headClip = AssetLoader.LoadOrNull<AudioClip>(audioPath);
                }
                else
                {
                    clip = AssetLoader.LoadOrNull<AudioClip>(audioPath);
                }
            }

            if (currentAudioName != lastAudioName)
            {
                if (clip != null)
                {
                    audioSource.SetClip(clip, headClip);
                    audioSource.Play();
                }
                else
                {
                    audioSource.Stop();
                    audioSource.SetClip(null, null);
                }
            }
            else // isPlaying != lastIsPlaying
            {
                if (isPlaying)
                {
                    audioSource.UnPause();
                }
                else
                {
                    audioSource.Pause();
                }
            }

            lastAudioName = currentAudioName;
            lastIsPlaying = isPlaying;
        }

        #region Methods called by external scripts

        // Play from the beginning
        public void Play(string audioName)
        {
            currentAudioName = audioName;
            isPlaying = true;
            ForceUpdate();
        }

        public void Stop()
        {
            currentAudioName = null;
            isPlaying = false;
            ForceUpdate();
        }

        public void Pause()
        {
            isPlaying = false;
            ForceUpdate();
        }

        public void UnPause()
        {
            isPlaying = true;
            ForceUpdate();
        }

        public void Preload(string audioName)
        {
            // To reduce the possibility of discontinuity between head and loop,
            // we use the full audio as head
            var audioPath = System.IO.Path.Combine(audioFolder, audioName);
            AssetLoader.Preload(AssetCacheType.Audio, audioPath);
            AssetLoader.Preload(AssetCacheType.Audio, audioPath + "_loop");
        }

        public void Unpreload(string audioName)
        {
            // To reduce the possibility of discontinuity between head and loop,
            // we use the full audio as head
            var audioPath = System.IO.Path.Combine(audioFolder, audioName);
            AssetLoader.Unpreload(AssetCacheType.Audio, audioPath);
            AssetLoader.Unpreload(AssetCacheType.Audio, audioPath + "_loop");
        }

        #endregion

        #region Restoration

        public string restorableName => luaGlobalName;

        /// <inheritdoc />
        /// <summary>
        /// Data used to restore the state of the audio controller
        /// </summary>
        [Serializable]
        private class AudioControllerRestoreData : IRestoreData
        {
            public readonly string currentAudioName;
            public readonly bool isPlaying;
            public readonly float scriptVolume;

            public AudioControllerRestoreData(string currentAudioName, bool isPlaying, float scriptVolume)
            {
                this.currentAudioName = currentAudioName;
                this.isPlaying = isPlaying;
                this.scriptVolume = scriptVolume;
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new AudioControllerRestoreData(currentAudioName, isPlaying, scriptVolume);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as AudioControllerRestoreData;
            currentAudioName = data.currentAudioName;
            isPlaying = data.isPlaying;
            scriptVolume = data.scriptVolume;
        }

        #endregion
    }
}
