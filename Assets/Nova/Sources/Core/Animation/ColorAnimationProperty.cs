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
            throw new NotImplementedException();
        }

        protected override Color Lerp(Color a, Color b, float t) => Color.LerpUnclamped(a, b, t);

        public ColorAnimationProperty(CharacterColor characterColor, Color startValue, Color targetValue) : base(
            startValue, targetValue)
        {
            this.characterColor = characterColor;
        }

        public ColorAnimationProperty(CharacterColor characterColor, Color targetValue) : base(targetValue)
        {
            this.characterColor = characterColor;
        }

        public ColorAnimationProperty(SpriteRenderer spriteRenderer, Color startValue, Color targetValue) : base(
            startValue, targetValue)
        {
            this.spriteRenderer = spriteRenderer;
        }

        public ColorAnimationProperty(SpriteRenderer spriteRenderer, Color targetValue) : base(targetValue)
        {
            this.spriteRenderer = spriteRenderer;
        }

        public ColorAnimationProperty(Image image, Color startValue, Color targetValue) : base(startValue, targetValue)
        {
            this.image = image;
        }

        public ColorAnimationProperty(Image image, Color targetValue) : base(targetValue)
        {
            this.image = image;
        }

        public ColorAnimationProperty(DialogueBoxColor dialogueBoxColor, Color startValue, Color targetValue) : base(
            startValue, targetValue)
        {
            this.dialogueBoxColor = dialogueBoxColor;
        }

        public ColorAnimationProperty(DialogueBoxColor dialogueBoxColor, Color targetValue) : base(targetValue)
        {
            this.dialogueBoxColor = dialogueBoxColor;
        }
    }
}
