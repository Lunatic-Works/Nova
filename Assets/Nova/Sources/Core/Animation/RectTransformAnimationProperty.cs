using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Position is the center of the element.
    /// </summary>
    public class RectTransformAnimationProperty : IAnimationProperty
    {
        private readonly RectTransform target;

        // TODO: lazy startValue
        private readonly Vector2 startPosition, targetPosition, startScale, targetScale;

        public RectTransformAnimationProperty(RectTransform target,
            Vector2 startPosition, Vector2 targetPosition)
        {
            this.target = target;
            this.startPosition = startPosition;
            this.targetPosition = targetPosition;
            startScale = targetScale = Vector2.one;
            // For UI animation, apply startValue when this is constructed
            value = 0.0f;
        }

        public RectTransformAnimationProperty(RectTransform target,
            Vector2 startPosition, Vector2 targetPosition, Vector2 startSize, Vector2 targetSize)
        {
            this.target = target;
            this.startPosition = startPosition;
            this.targetPosition = targetPosition;
            var baseSize = target.rect.size;
            startScale = startSize.InverseScale(baseSize);
            targetScale = targetSize.InverseScale(baseSize);
            // For UI animation, apply startValue when this is constructed
            value = 0.0f;
        }

        private float _value = 0.0f;

        public float value
        {
            get => _value;
            set
            {
                _value = value;
                Vector3 pos = Vector2.LerpUnclamped(startPosition, targetPosition, value);
                pos.z = target.position.z;
                target.position = pos;
                Vector3 scale = Vector2.LerpUnclamped(startScale, targetScale, value);
                scale.z = 1.0f;
                target.localScale = scale;
            }
        }
    }
}