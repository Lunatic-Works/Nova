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

        public float volume
        {
            get { return audioSource.volume; }
            set { audioSource.volume = value; }
        }

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            LuaRuntime.Instance.BindObject(audioControllerName, this);
        }

        private AudioClip GetAudioClip(string audioName)
        {
            audioName = audioPath + audioName;
            return AssetsLoader.GetAudioClip(audioName);
        }


        public void PlayAudio(string audioName)
        {
            var audio = GetAudioClip(audioName);
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

        public void PlayClipAtPoint(string audioName, Vector3 position, float clipVolume)
        {
            var audio = GetAudioClip(audioName);
            AudioSource.PlayClipAtPoint(audio, position, clipVolume * volume);
        }
    }
}