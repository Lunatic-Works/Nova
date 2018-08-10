using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Nova.Examples.Colorless.Scripts
{
    /// <summary>
    /// Ugly implementation only used for demo.
    /// </summary>
    public class InterludeController : MonoBehaviour
    {
        public string chapterSpritePath;

        [SerializeField] private float fadeDuration;

        [SerializeField] public float holdingDuration;

        public Sprite BackgroundSprite;

        private GameState _gameState;

        private SpriteChangerPreserveAspectRatio _chapterSpriteChanger;

        private SpriteChangerWithFade _backgroundChanger;

        private void Awake()
        {
            _gameState = Utils.FindNovaGameController().GetComponent<GameState>();
            _gameState.NodeChanged += OnNodeChanged;

            var image = transform.Find("InterludePanel/Image");
            _chapterSpriteChanger = image.GetComponent<SpriteChangerPreserveAspectRatio>();
            image.GetComponent<SpriteChangerWithFade>().fadeDuration = fadeDuration;

            _backgroundChanger = transform.Find("InterludePanel").GetComponent<SpriteChangerWithFade>();
            _backgroundChanger.fadeDuration = fadeDuration;

            LuaRuntime.Instance.BindObject("interlude", this, "_G");
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _gameState.NodeChanged -= OnNodeChanged;
        }

        private string currentNodeName;

        private void OnNodeChanged(NodeChangedData data)
        {
            currentNodeName = data.nodeName;
        }


        /// <summary>
        /// Method called by external script
        /// </summary>
        public void Play()
        {
            _gameState.ActionAquirePause();
            gameObject.SetActive(true);
            StartCoroutine(PlayCoroutine());
        }


        private IEnumerator PlayCoroutine()
        {
            var chapterSprite = AssetsLoader.GetSprite(Path.Combine(chapterSpritePath, currentNodeName));
            Assert.IsNotNull(chapterSprite);
            // fade in
            _backgroundChanger.sprite = BackgroundSprite;
            _chapterSpriteChanger.sprite = chapterSprite;
            yield return new WaitForSeconds(fadeDuration);
            // hold
            yield return new WaitForSeconds(holdingDuration);
            // fade out
            _backgroundChanger.sprite = null;
            _chapterSpriteChanger.sprite = null;
            yield return new WaitForSeconds(fadeDuration);
            gameObject.SetActive(false);
            _gameState.ActionReleasePause();
        }
    }
}