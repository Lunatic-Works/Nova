using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    /// <summary>
    /// Change the sprite of component Image or SpriteRenderer with fade effects
    /// </summary>
    public class SpriteChangerWithFade : MonoBehaviour
    {
        public bool needFade = true;
        public float fadeDuration = 0.1f;

        /// <summary>
        /// target alpha when no change happens
        /// </summary>
        public float staticAlpha = 1.0f;

        /// <summary>
        /// intermediate alpha for during the change
        /// </summary>
        public float intermediateAlpha = 0.0f;

        private delegate Sprite GetSprite();

        private delegate void SetSprite(Sprite sprite);

        private GetSprite _getSprite;
        private SetSprite _setSprite;

        private delegate Color GetColor();

        private delegate void SetColor(Color color);

        private GetColor _getColor;
        private SetColor _setColor;

        private void Awake()
        {
            var image = GetComponent<Image>();
            if (image != null)
            {
                _getSprite = () => image.sprite;
                _setSprite = sp => image.sprite = sp;
                _getColor = () => image.color;
                _setColor = c => image.color = c;
                return;
            }

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                _getSprite = () => spriteRenderer.sprite;
                _setSprite = sp => spriteRenderer.sprite = sp;
                _getColor = () => spriteRenderer.color;
                _setColor = c => spriteRenderer.color = c;
                return;
            }

            Debug.LogError(string.Format(
                "Nova: ChangeSpriteWithFade should have Image or SpriteRenderer component attached." +
                " Object name: {0}", gameObject.name));
        }

        private Coroutine fadingCoroutine;

        /// <summary>
        /// Assign this property to change the sprite with fade in/out effects
        /// </summary>
        public Sprite sprite
        {
            get { return _getSprite(); }
            set
            {
                if (!needFade)
                {
                    _setSprite(value);
                    return;
                }

                StartCoroutine(ChangeSpriteWithFade(value));
            }
        }

        private void Fade(float fromAlpha, float toAlpha, float time)
        {
            iTween.ValueTo(gameObject, iTween.Hash(
                "from", fromAlpha,
                "to", toAlpha,
                "time", time,
                "easetype", "linear",
                "onupdate", "SetAlpha"
            ));
        }

        private void SetAlpha(float newAlpha)
        {
            var oldColor = _getColor();
            _setColor(new Color(oldColor.r, oldColor.g, oldColor.b, newAlpha));
        }

        private IEnumerator ChangeSpriteWithFade(Sprite sp)
        {
            if (_getSprite != null)
            {
                // Fade out if previous sprite exists
                Fade(staticAlpha, intermediateAlpha, fadeDuration);
                yield return new WaitForSeconds(fadeDuration);
            }

            _setSprite(sp);
            Fade(intermediateAlpha, staticAlpha, fadeDuration);
        }
    }
}