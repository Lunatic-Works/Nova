using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class OpacityAnimationProperty : IAnimationProperty
    {
        private readonly SpriteRenderer spriteRenderer;
        private readonly Image image;
        private readonly CanvasGroup canvasGroup;
        private readonly RawImage rawImage;

        private float opacity
        {
            get
            {
                if (spriteRenderer != null)
                {
                    return spriteRenderer.color.a;
                }
                else if (image != null)
                {
                    return image.color.a;
                }
                else if (canvasGroup != null)
                {
                    return canvasGroup.alpha;
                }
                else
                {
                    return rawImage.color.a;
                }
            }
            set
            {
                if (spriteRenderer != null)
                {
                    Color c = spriteRenderer.color;
                    c.a = value;
                    spriteRenderer.color = c;
                }
                else if (image != null)
                {
                    Color c = image.color;
                    c.a = value;
                    image.color = c;
                }
                else if (canvasGroup != null)
                {
                    canvasGroup.alpha = value;
                }
                else
                {
                    Color c = rawImage.color;
                    c.a = value;
                    rawImage.color = c;
                }
            }
        }

        // TODO: lazy startValue
        private readonly float startValue, targetValue;

        public OpacityAnimationProperty(SpriteRenderer spriteRenderer, float startValue, float targetValue)
        {
            this.spriteRenderer = spriteRenderer;
            this.startValue = startValue;
            this.targetValue = targetValue;
        }

        public OpacityAnimationProperty(Image image, float startValue, float targetValue)
        {
            this.image = image;
            this.startValue = startValue;
            this.targetValue = targetValue;
        }

        public OpacityAnimationProperty(CanvasGroup canvasGroup, float startValue, float targetValue)
        {
            this.canvasGroup = canvasGroup;
            this.startValue = startValue;
            this.targetValue = targetValue;
            // For UI animation, apply startValue when this is constructed
            value = 0;
        }

        public OpacityAnimationProperty(RawImage rawImage, float startValue, float targetValue)
        {
            this.rawImage = rawImage;
            this.startValue = startValue;
            this.targetValue = targetValue;
            // For UI animation, apply startValue when this is constructed
            value = 0;
        }

        public string id => "Opacity";

        public float value
        {
            get => Mathf.InverseLerp(startValue, targetValue, opacity);
            set => opacity = Mathf.LerpUnclamped(startValue, targetValue, value);
        }
    }
}