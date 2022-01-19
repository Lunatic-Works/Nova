using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [Obsolete]
    public class SpriteChangerWithFade : MonoBehaviour
    {
        public float fadeDuration = 0.1f;
        public float fadeInDelay = 0.0f;

        private SpriteRenderer spriteRenderer;
        private Image image;
        private NovaAnimation novaAnimation;
        private GameObject ghost;
        private SpriteRenderer ghostSpriteRenderer;
        private Image ghostImage;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            image = GetComponent<Image>();
            this.RuntimeAssert(spriteRenderer != null || image != null, "Missing SpriteRenderer or Image.");
            novaAnimation = Utils.FindNovaGameController().PerDialogueAnimation;
            ghost = new GameObject("SpriteChangerGhost");
            ghost.transform.SetParent(transform);
            ghost.SetActive(false);
            // Show ghost in front of original sprite
            ghost.transform.localPosition = 0.001f * Vector3.back;
            if (spriteRenderer != null)
            {
                ghostSpriteRenderer = ghost.AddComponent<SpriteRenderer>();
            }
            else
            {
                ghostImage = ghost.AddComponent<Image>();
                RectTransform rectTransform = GetComponent<RectTransform>();
                RectTransform ghostRectTransform = ghost.GetComponent<RectTransform>();
                ghostRectTransform.sizeDelta = rectTransform.sizeDelta;
            }
        }

        private void SetSpriteRenderer(Sprite value, bool overlay)
        {
            Color color = spriteRenderer.color;
            var colorTo = new Color(color.r, color.g, color.b, 0.0f);

            bool needFadeInDelay = false;
            if (spriteRenderer.sprite != null)
            {
                needFadeInDelay = true;
                ghostSpriteRenderer.sprite = spriteRenderer.sprite;
                ghostSpriteRenderer.color = spriteRenderer.color;
                ghostSpriteRenderer.material = spriteRenderer.material;
                ghost.SetActive(true);
                novaAnimation.Do(new OpacityAnimationProperty(ghostSpriteRenderer, color.a, colorTo.a), fadeDuration)
                    .Then(new ActionAnimationProperty(() => ghost.SetActive(false)));
            }

            if (value != null)
            {
                spriteRenderer.sprite = value;
                if (!overlay)
                {
                    spriteRenderer.color = colorTo;
                    if (needFadeInDelay)
                    {
                        novaAnimation.Do(null, fadeInDelay)
                            .Then(new OpacityAnimationProperty(spriteRenderer, colorTo.a, color.a), fadeDuration);
                    }
                    else
                    {
                        novaAnimation.Do(new OpacityAnimationProperty(spriteRenderer, colorTo.a, color.a),
                            fadeDuration);
                    }
                }
            }
            else
            {
                spriteRenderer.sprite = null;
            }
        }

        private void SetImage(Sprite value, bool overlay)
        {
            Color color = image.color;
            var colorTo = new Color(color.r, color.g, color.b, 0.0f);

            bool needFadeInDelay = false;
            if (image.sprite != null)
            {
                needFadeInDelay = true;
                ghostImage.sprite = image.sprite;
                ghostImage.SetNativeSize();
                ghostImage.color = image.color;
                ghostImage.material = image.material;
                ghost.SetActive(true);
                novaAnimation.Do(new OpacityAnimationProperty(ghostImage, color.a, colorTo.a), fadeDuration)
                    .Then(new ActionAnimationProperty(() => ghost.SetActive(false)));
            }

            if (value != null)
            {
                image.sprite = value;
                image.SetNativeSize();
                if (!overlay)
                {
                    image.color = colorTo;
                    if (needFadeInDelay)
                    {
                        novaAnimation.Do(null, fadeInDelay)
                            .Then(new OpacityAnimationProperty(image, colorTo.a, color.a), fadeDuration);
                    }
                    else
                    {
                        novaAnimation.Do(new OpacityAnimationProperty(image, colorTo.a, color.a), fadeDuration);
                    }
                }
            }
            else
            {
                image.sprite = null;
            }
        }

        public void SetSprite(Sprite value, bool overlay)
        {
            if (spriteRenderer != null)
            {
                SetSpriteRenderer(value, overlay);
            }
            else
            {
                SetImage(value, overlay);
            }
        }

        public Sprite sprite
        {
            get
            {
                if (spriteRenderer != null)
                {
                    return spriteRenderer.sprite;
                }
                else
                {
                    return image.sprite;
                }
            }
            set => SetSprite(value, overlay: false);
        }
    }
}