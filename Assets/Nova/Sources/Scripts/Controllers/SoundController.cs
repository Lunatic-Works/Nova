// TODO: Use an object pool for custom one shot sound
// TODO: Send to audio mixer

using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class SoundController : MonoBehaviour
    {
        public string luaName;
        public string audioFolder;
        [HideInInspector] public float configVolume;

        private GameState gameState;

        private void Awake()
        {
            gameState = Utils.FindNovaController().GameState;

            if (!string.IsNullOrEmpty(luaName))
            {
                LuaRuntime.Instance.BindObject(luaName, this);
            }
        }

        private bool dontPlaySound => gameState.isRestoring || gameState.isJumping;

        /// <summary>
        /// Play audio at a point in space, handy for sound effect
        /// </summary>
        /// <param name="clip">the AudioClip to play</param>
        /// <param name="position">the position to play the audio</param>
        /// <param name="clipVolume">the volume to play the clip</param>
        public void PlayClipAtPoint(AudioClip clip, Vector3 position, float clipVolume)
        {
            if (dontPlaySound) return;
            var volume = Utils.LogToLinearVolume(clipVolume * configVolume);
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        public void PlayClipAtPoint(string audioName, Vector3 position, float clipVolume)
        {
            if (dontPlaySound) return;
            var clip = AssetLoader.Load<AudioClip>(System.IO.Path.Combine(audioFolder, audioName));
            PlayClipAtPoint(clip, position, clipVolume);
        }

        public void PlayClipNo3D(AudioClip clip, Vector3 position, float clipVolume)
        {
            if (dontPlaySound) return;
            var go = new GameObject("Custom one shot sound");
            go.transform.position = position;
            var audioSource = go.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = Utils.LogToLinearVolume(clipVolume * configVolume);
            audioSource.spatialBlend = 0.0f;
            audioSource.Play();
            Destroy(go, clip.length);
        }

        public void PlayClipNo3D(string audioName, Vector3 position, float clipVolume)
        {
            if (dontPlaySound) return;
            var clip = AssetLoader.Load<AudioClip>(System.IO.Path.Combine(audioFolder, audioName));
            PlayClipNo3D(clip, position, clipVolume);
        }
    }
}
