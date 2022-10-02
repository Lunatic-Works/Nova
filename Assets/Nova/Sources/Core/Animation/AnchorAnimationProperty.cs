using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class AnchorAnimationProperty : LazyComputableAnimationProperty<Vector4, Vector4>
    {
        private readonly RectTransform rect;

        protected override Vector4 currentValue
        {
            get
            {
                var anchorMin = rect.anchorMin;
                var anchorMax = rect.anchorMax;
                return new Vector4(anchorMin.x, anchorMax.x, anchorMin.y, anchorMax.y);
            }
            set
            {
                rect.anchorMin = new Vector2(value.x, value.z);
                rect.anchorMax = new Vector2(value.y, value.w);
            }
        }

        protected override Vector4 CombineDelta(Vector4 a, Vector4 b) => a + b;

        protected override Vector4 Lerp(Vector4 a, Vector4 b, float t) => Vector4.LerpUnclamped(a, b, t);

        public AnchorAnimationProperty(RectTransform rect, Vector4 startValue, Vector4 targetValue) : base(startValue,
            targetValue)
        {
            this.rect = rect;
        }

        public AnchorAnimationProperty(RectTransform rect, Vector4 targetValue) : base(targetValue)
        {
            this.rect = rect;
        }

        public AnchorAnimationProperty(RectTransform rect, Vector4 deltaValue, UseRelativeValue useRelativeValue) :
            base(deltaValue, useRelativeValue)
        {
            this.rect = rect;
        }
    }
}
