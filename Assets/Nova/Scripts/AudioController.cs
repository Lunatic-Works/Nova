using UnityEditor;
using UnityEngine;

namespace Nova
{
    /// <inheritdoc />
    /// <summary>
    /// This class is used for controlling audio source from external scripts
    /// </summary>
    public class AudioController : MonoBehaviour
    {
        /// <summary>
        /// Name used to bind it self to Lua scripts
        /// </summary>
        public string audioControllerName;


        /// <summary>
        /// The path to the audio files
        /// </summary>
        public string audioPath;

        private AudioSource audioSource;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            LuaRuntime.Instance.BindObject(audioControllerName, this);
        }

        #region Methods called by external scripts

        public void PlayAudio(string audioName)
        {
            audioName = audioPath + audioName;
            var audio = AssetsLoader.GetAudioClip(audioName);
            StopAudio();
            audioSource.clip = audio;
            audioSource.Play();
        }

        public void StopAudio()
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        #endregion
    }
}