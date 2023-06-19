using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [ExportCustomType]
    public class OpacityAnimationProperty : FloatBaseAnimationProperty
    {
        private readonly SpriteRenderer spriteRenderer;
        private readonly FadeController fadeController;
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
                else if (fadeController != null)
                {
                    return fadeController.color.a;
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
                    spriteRenderer.color = Utils.SetAlpha(spriteRenderer.color, value);
                }
                else if (fadeController != null)
                {
                    fadeController.color = Utils.SetAlpha(fadeController.color, value);
                }
                else if (image != null)
                {
                    image.color = Utils.SetAlpha(image.color, value);
                }
                else if (canvasGroup != null)
                {
                    canvasGroup.alpha = value;
                }
                else
                {
                    rawImage.color = Utils.SetAlpha(rawImage.color, value);
                }
            }
        }

        public OpacityAnimationProperty(SpriteRenderer spriteRenderer, float startValue, float targetValue) : base(
            startValue, targetValue)
        {
            this.spriteRenderer = spriteRenderer;
        }

        public OpacityAnimationProperty(SpriteRenderer spriteRenderer, float targetValue) : base(targetValue)
        {
            this.spriteRenderer = spriteRenderer;
        }

        public OpacityAnimationProperty(SpriteRenderer spriteRenderer, float deltaValue, UseRelativeValue
            useRelativeValue) : base(deltaValue, useRelativeValue)
        {
            this.spriteRenderer = spriteRenderer;
        }

        public OpacityAnimationProperty(FadeController fadeController, float startValue, float targetValue) : base(
            startValue, targetValue)
        {
            this.fadeController = fadeController;
        }

        public OpacityAnimationProperty(FadeController fadeController, float targetValue) : base(targetValue)
        {
            this.fadeController = fadeController;
        }

        public OpacityAnimationProperty(FadeController fadeController, float deltaValue, UseRelativeValue
            useRelativeValue) : base(deltaValue, useRelativeValue)
        {
            this.fadeController = fadeController;
        }

        public OpacityAnimationProperty(Image image, float startValue, float targetValue) : base(startValue,
            targetValue)
        {
            this.image = image;
        }

        public OpacityAnimationProperty(Image image, float targetValue) : base(targetValue)
        {
            this.image = image;
        }

        public OpacityAnimationProperty(Image image, float deltaValue, UseRelativeValue useRelativeValue) : base(
            deltaValue, useRelativeValue)
        {
            this.image = image;
        }

        public OpacityAnimationProperty(CanvasGroup canvasGroup, float startValue, float targetValue) : base(startValue,
            targetValue)
        {
            this.canvasGroup = canvasGroup;
            // For UI animation, apply startValue when this is constructed
            value = 0f;
        }

        public OpacityAnimationProperty(RawImage rawImage, float startValue, float targetValue) : base(startValue,
            targetValue)
        {
            this.rawImage = rawImage;
            // For UI animation, apply startValue when this is constructed
            value = 0f;
        }
    }
}
