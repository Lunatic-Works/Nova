using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class OffsetAnimationProperty : LazyComputableAnimationProperty<Vector4, Vector4>
    {
        private readonly RectTransform rect;

        protected override Vector4 currentValue
        {
            get
            {
                var offsetMin = rect.offsetMin;
                var offsetMax = rect.offsetMax;
                return new Vector4(offsetMin.x, -offsetMax.x, -offsetMax.y, offsetMin.y);
            }
            set
            {
                rect.offsetMin = new Vector2(value.x, value.w);
                rect.offsetMax = new Vector2(-value.y, -value.z);
            }
        }

        protected override Vector4 CombineDelta(Vector4 a, Vector4 b) => a + b;

        protected override Vector4 Lerp(Vector4 a, Vector4 b, float t) => Vector4.LerpUnclamped(a, b, t);

        public OffsetAnimationProperty(RectTransform rect, Vector4 startValue, Vector4 targetValue) : base(startValue,
            targetValue)
        {
            this.rect = rect;
        }

        public OffsetAnimationProperty(RectTransform rect, Vector4 targetValue) : base(targetValue)
        {
            this.rect = rect;
        }

        public OffsetAnimationProperty(RectTransform rect, Vector4 deltaValue, UseRelativeValue useRelativeValue) :
            base(deltaValue, useRelativeValue)
        {
            this.rect = rect;
        }
    }
}
