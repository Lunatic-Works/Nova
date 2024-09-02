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

        // For debug
        [SerializeField] private Slider debugSlider;
        [SerializeField] private Text debugText;

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

        public float pitch
        {
            get => audioSource.pitch;
            set => audioSource.pitch = value;
        }

        private bool inited;

        private void Init()
        {
            if (inited)
            {
                return;
            }

            gameState = Utils.FindNovaController().GameState;

            var _audioSource = GetComponent<AudioSource>();
            if (_audioSource.loop)
            {
                audioSource = gameObject.AddComponent<AudioLooper>();
            }
            else
            {
                audioSource = _audioSource;
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
                if (debugSlider != null)
                {
                    debugSlider.value = (float)audioSource.timeSamples / audioSource.clip.samples;
                }

                if (debugText != null)
                {
                    debugText.text = $"{audioSource.timeSamples} / {audioSource.clip.samples}";
                }
            }

            if (currentAudioName == lastAudioName && isPlaying == lastIsPlaying) return;
            ForceUpdate();
        }

        private void ForceUpdate()
        {
            AudioClip clip = null;
            MusicEntry musicEntry = null;

            if (!string.IsNullOrEmpty(currentAudioName))
            {
                var audioPath = System.IO.Path.Combine(audioFolder, currentAudioName);
                clip = AssetLoader.Load<AudioClip>(audioPath);
                musicEntry = AssetLoader.LoadOrNull<MusicEntry>(audioPath + "_entry");
            }

            if (currentAudioName != lastAudioName)
            {
                if (clip != null)
                {
                    audioSource.SetClip(clip, musicEntry);
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

        public void ResetTime()
        {
            audioSource.timeSamples = 0;
        }

        public void Preload(string audioName)
        {
            var audioPath = System.IO.Path.Combine(audioFolder, audioName);
            AssetLoader.Preload(AssetCacheType.Audio, audioPath);
        }

        public void Unpreload(string audioName)
        {
            var audioPath = System.IO.Path.Combine(audioFolder, audioName);
            AssetLoader.Unpreload(AssetCacheType.Audio, audioPath);
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
            public readonly float pitch;

            public AudioControllerRestoreData(AudioController parent)
            {
                currentAudioName = parent.currentAudioName;
                isPlaying = parent.isPlaying;
                scriptVolume = parent.scriptVolume;
                pitch = parent.pitch;
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new AudioControllerRestoreData(this);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as AudioControllerRestoreData;
            currentAudioName = data.currentAudioName;
            isPlaying = data.isPlaying;
            scriptVolume = data.scriptVolume;
            pitch = data.pitch;
        }

        #endregion
    }
}
