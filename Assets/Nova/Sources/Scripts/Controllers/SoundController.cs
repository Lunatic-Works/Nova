using System.Collections;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class SoundController : MonoBehaviour
    {
        private static PrefabFactory PrefabFactory;

        [SerializeField] private string luaName;
        [SerializeField] private string audioFolder;
        [SerializeField] private GameObject oneShotSoundPrefab;

        [HideInInspector] public float configVolume;

        private GameState gameState;

        private void Awake()
        {
            if (PrefabFactory == null)
            {
                var go = new GameObject("OneShotSoundFactory");
                PrefabFactory = go.AddComponent<PrefabFactory>();
                PrefabFactory.prefab = oneShotSoundPrefab;
                PrefabFactory.maxBufferSize = 100;
            }

            gameState = Utils.FindNovaController().GameState;

            if (!string.IsNullOrEmpty(luaName))
            {
                LuaRuntime.Instance.BindObject(luaName, this);
            }
        }

        private bool dontPlaySound => gameState.isRestoring || gameState.isJumping;

        public void PlayClip(AudioClip clip, float clipVolume, Vector3 position, bool is3D)
        {
            if (dontPlaySound) return;

            var audioSource = PrefabFactory.Get<AudioSource>();
            audioSource.transform.position = position;
            audioSource.clip = clip;
            audioSource.volume = Utils.LogToLinearVolume(clipVolume * configVolume);
            if (is3D)
            {
                audioSource.spatialBlend = 1.0f;
            }
            else
            {
            audioSource.spatialBlend = 0.0f;
        }

            audioSource.Play();
            StartCoroutine(WaitAndDestroy(audioSource, clip.length));
        }

        public void PlayClip(string audioName, float clipVolume, Vector3 position, bool is3D)
        {
            if (dontPlaySound) return;
            var clip = AssetLoader.Load<AudioClip>(System.IO.Path.Combine(audioFolder, audioName));
            PlayClip(clip, clipVolume, position, is3D);
        }

        private IEnumerator WaitAndDestroy(AudioSource audioSource, float time)
        {
            yield return new WaitForSeconds(time);
            audioSource.Stop();
            audioSource.clip = null;
            PrefabFactory.Put(audioSource.gameObject);
        }
    }
}
