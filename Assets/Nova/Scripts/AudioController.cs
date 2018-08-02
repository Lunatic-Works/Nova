using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Assertions;

namespace Nova
{
    /// <inheritdoc />
    /// <summary>
    /// This class is used for controlling audio source from external scripts
    /// </summary>
    public class AudioController : MonoBehaviour, IRestorable
    {
        /// <summary>
        /// Name used to bind it self to Lua scripts, and served as the restorable name, which should be unique
        /// </summary>
        public string audioControllerName;

        /// <summary>
        /// The path to the audio files
        /// </summary>
        public string audioPath;

        private AudioSource audioSource;

        public GameState gameState;

        public float volume
        {
            get { return audioSource.volume; }
            set { audioSource.volume = value; }
        }

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            LuaRuntime.Instance.BindObject(audioControllerName, this);
            gameState.AddRestorable(this);
        }

        private AudioClip GetAudioClip(string audioName)
        {
            audioName = audioPath + audioName;
            var audio = AssetsLoader.GetAudioClip(audioName);
            Assert.IsNotNull(audio);
            return audio;
        }

        private string currentAudioName;

        /// <summary>
        /// Play the audio
        /// </summary>
        /// <param name="audioName">the name of the audio</param>
        public void PlayAudio(string audioName)
        {
            var audio = GetAudioClip(audioName);
            StopAudio();
            audioSource.clip = audio;
            currentAudioName = audioName;
            audioSource.Play();
        }

        /// <summary>
        /// Stop Audio
        /// </summary>
        public void StopAudio()
        {
            if (!audioSource.isPlaying) return;
            audioSource.Stop();
        }

        /// <summary>
        /// Play music at a point in space, handy for sound effect
        /// </summary>
        /// <param name="audioName">the name of the audio to play</param>
        /// <param name="position">the position to play the audio</param>
        /// <param name="clipVolume">the volumn to play the clip</param>
        public void PlayClipAtPoint(string audioName, Vector3 position, float clipVolume)
        {
            var audio = GetAudioClip(audioName);
            AudioSource.PlayClipAtPoint(audio, position, clipVolume * volume);
        }

        /// <inheritdoc />
        /// <summary>
        /// Data used to restore the state of the audio controller
        /// </summary>
        [Serializable]
        private class RestoreData : IRestoreData
        {
            public string audioName { get; private set; }
            public bool isPlaying { get; private set; }

            public RestoreData(string audioName, bool isPlaying)
            {
                this.audioName = audioName;
                this.isPlaying = isPlaying;
            }
        }

        public string restorableObjectName
        {
            get { return audioControllerName; }
        }

        public IRestoreData GetRestoreData()
        {
            return new RestoreData(currentAudioName, audioSource.isPlaying);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as RestoreData;
            if (data.isPlaying)
            {
                if (currentAudioName != data.audioName || !audioSource.isPlaying)
                {
                    PlayAudio(data.audioName);
                }
            }
            else
            {
                StopAudio();
                currentAudioName = data.audioName;
            }
        }
    }
}