using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class PositionAnimationProperty : LazyComputableAnimationProperty<Vector3, Vector3>
    {
        private readonly Transform transform;

        protected override Vector3 currentValue
        {
            get => transform.localPosition;
            set => transform.localPosition = value;
        }

        protected override Vector3 CombineDelta(Vector3 a, Vector3 b) => a + b;

        protected override Vector3 Lerp(Vector3 a, Vector3 b, float t) => Vector3.LerpUnclamped(a, b, t);

        public PositionAnimationProperty(Transform transform, Vector3 startValue, Vector3 targetValue) : base(
            startValue, targetValue)
        {
            this.transform = transform;
        }

        public PositionAnimationProperty(Transform transform, Vector3 targetValue) : base(targetValue)
        {
            this.transform = transform;
        }

        public PositionAnimationProperty(Transform transform, Vector3 deltaValue, UseRelativeValue useRelativeValue) :
            base(deltaValue, useRelativeValue)
        {
            this.transform = transform;
        }
    }
}
