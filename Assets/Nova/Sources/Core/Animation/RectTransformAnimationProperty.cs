using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Position is the center of the element.
    /// </summary>
    [ExportCustomType]
    public class RectTransformAnimationProperty : IAnimationProperty
    {
        private readonly RectTransform rect;

        // TODO: lazy startValue
        private readonly Vector2 startPosition, targetPosition, startScale, targetScale;

        private readonly bool useLocalPosition;

        public RectTransformAnimationProperty(RectTransform rect,
            Vector2 startPosition, Vector2 targetPosition, bool useLocalPosition = false)
        {
            this.rect = rect;
            this.startPosition = startPosition;
            this.targetPosition = targetPosition;
            startScale = targetScale = Vector2.one;
            this.useLocalPosition = useLocalPosition;
            // For UI animation, apply startValue when this is constructed
            value = 0f;
        }

        public RectTransformAnimationProperty(RectTransform rect,
            Vector2 startPosition, Vector2 targetPosition, Vector2 startSize, Vector2 targetSize,
            bool useLocalPosition = false)
        {
            this.rect = rect;
            this.startPosition = startPosition;
            this.targetPosition = targetPosition;
            var baseSize = rect.rect.size;
            startScale = startSize.InverseScale(baseSize);
            targetScale = targetSize.InverseScale(baseSize);
            this.useLocalPosition = useLocalPosition;
            // For UI animation, apply startValue when this is constructed
            value = 0f;
        }

        private float _value;

        public float value
        {
            get => _value;
            set
            {
                _value = value;
                Vector3 pos = Vector2.LerpUnclamped(startPosition, targetPosition, value);
                if (useLocalPosition)
                {
                    pos.z = rect.localPosition.z;
                    rect.localPosition = pos;
                }
                else
                {
                    pos.z = rect.position.z;
                    rect.position = pos;
                }

                Vector3 scale = Vector2.LerpUnclamped(startScale, targetScale, value);
                scale.z = rect.localScale.z;
                rect.localScale = scale;
            }
        }
    }
}
