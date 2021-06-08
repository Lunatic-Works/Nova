using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class OpacityAnimationProperty : FloatBaseAnimationProperty
    {
        private readonly SpriteRenderer spriteRenderer;
        private readonly Image image;
        private readonly CanvasGroup canvasGroup;
        private readonly RawImage rawImage;

        protected override float currentValue
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

        public OpacityAnimationProperty(SpriteRenderer spriteRenderer, float startValue, float targetValue) : base(startValue, targetValue)
        {
            this.spriteRenderer = spriteRenderer;
        }

        public OpacityAnimationProperty(Image image, float startValue, float targetValue) : base(startValue, targetValue)
        {
            this.image = image;
        }

        public OpacityAnimationProperty(CanvasGroup canvasGroup, float startValue, float targetValue) : base(startValue, targetValue)
        {
            this.canvasGroup = canvasGroup;
            // For UI animation, apply startValue when this is constructed
            value = 0.0f;
        }

        public OpacityAnimationProperty(RawImage rawImage, float startValue, float targetValue) : base(startValue, targetValue)
        {
            this.rawImage = rawImage;
            // For UI animation, apply startValue when this is constructed
            value = 0.0f;
        }
    }
}