using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [ExportCustomType]
    public class ColorAnimationProperty : LazyComputableAnimationProperty<Color, Color>
    {
        private readonly CharacterColor characterColor;
        private readonly SpriteRenderer spriteRenderer;
        private readonly FadeController fadeController;
        private readonly Image image;
        private readonly DialogueBoxColor dialogueBoxColor;

        protected override Color currentValue
        {
            get
            {
                if (characterColor != null)
                {
                    return characterColor.color;
                }
                else if (spriteRenderer != null)
                {
                    return spriteRenderer.color;
                }
                else if (fadeController != null)
                {
                    return fadeController.color;
                }
                else if (image != null)
                {
                    return image.color;
                }
                else
                {
                    return dialogueBoxColor.color;
                }
            }
            set
            {
                if (characterColor != null)
                {
                    characterColor.color = value;
                }
                else if (spriteRenderer != null)
                {
                    spriteRenderer.color = value;
                }
                else if (fadeController != null)
                {
                    fadeController.color = value;
                }
                else if (image != null)
                {
                    image.color = value;
                }
                else
                {
                    dialogueBoxColor.color = value;
                }
            }
        }

        protected override Color CombineDelta(Color a, Color b)
        {
            // Colors should not be simply added
            throw new NotImplementedException();
        }

        protected override Color Lerp(Color a, Color b, float t) => Color.LerpUnclamped(a, b, t);

        public ColorAnimationProperty(CharacterColor characterColor, Color startValue, Color targetValue) :
            base(nameof(ColorAnimationProperty) + ":" + characterColor, startValue, targetValue)
        {
            this.characterColor = characterColor;
        }

        public ColorAnimationProperty(CharacterColor characterColor, Color targetValue) :
            base(nameof(ColorAnimationProperty) + ":" + characterColor, targetValue)
        {
            this.characterColor = characterColor;
        }

        public ColorAnimationProperty(SpriteRenderer spriteRenderer, Color startValue, Color targetValue) :
            base(nameof(ColorAnimationProperty) + ":" + Utils.GetPath(spriteRenderer), startValue, targetValue)
        {
            this.spriteRenderer = spriteRenderer;
        }

        public ColorAnimationProperty(SpriteRenderer spriteRenderer, Color targetValue) :
            base(nameof(ColorAnimationProperty) + ":" + Utils.GetPath(spriteRenderer), targetValue)
        {
            this.spriteRenderer = spriteRenderer;
        }

        public ColorAnimationProperty(FadeController fadeController, Color startValue, Color targetValue) :
            base(nameof(ColorAnimationProperty) + ":" + Utils.GetPath(fadeController), startValue, targetValue)
        {
            this.fadeController = fadeController;
        }

        public ColorAnimationProperty(FadeController fadeController, Color targetValue) :
            base(nameof(ColorAnimationProperty) + ":" + Utils.GetPath(fadeController), targetValue)
        {
            this.fadeController = fadeController;
        }

        public ColorAnimationProperty(Image image, Color startValue, Color targetValue) :
            base(nameof(ColorAnimationProperty) + ":" + Utils.GetPath(image), startValue, targetValue)
        {
            this.image = image;
        }

        public ColorAnimationProperty(Image image, Color targetValue) :
            base(nameof(ColorAnimationProperty) + ":" + Utils.GetPath(image), targetValue)
        {
            this.image = image;
        }

        public ColorAnimationProperty(DialogueBoxColor dialogueBoxColor, Color startValue, Color targetValue) :
            base(nameof(ColorAnimationProperty) + ":" + dialogueBoxColor, startValue, targetValue)
        {
            this.dialogueBoxColor = dialogueBoxColor;
        }

        public ColorAnimationProperty(DialogueBoxColor dialogueBoxColor, Color targetValue) :
            base(nameof(ColorAnimationProperty) + ":" + dialogueBoxColor, targetValue)
        {
            this.dialogueBoxColor = dialogueBoxColor;
        }
    }
}
