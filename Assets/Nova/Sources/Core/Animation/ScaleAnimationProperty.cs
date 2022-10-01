using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class ScaleAnimationProperty : LazyComputableAnimationProperty<Vector3, Vector3>
    {
        private readonly Transform transform;

        protected override Vector3 currentValue
        {
            get => transform.localScale;
            set => transform.localScale = value;
        }

        protected override Vector3 CombineDelta(Vector3 a, Vector3 b) => a.CloneScale(b);

        protected override Vector3 Lerp(Vector3 a, Vector3 b, float t) => Vector3.LerpUnclamped(a, b, t);

        public ScaleAnimationProperty(Transform transform, Vector3 startValue, Vector3 targetValue) : base(startValue,
            targetValue)
        {
            this.transform = transform;
        }

        public ScaleAnimationProperty(Transform transform, Vector3 targetValue) : base(targetValue)
        {
            this.transform = transform;
        }

        public ScaleAnimationProperty(Transform transform, Vector3 deltaValue, UseRelativeValue useRelativeValue) :
            base(deltaValue, useRelativeValue)
        {
            this.transform = transform;
        }
    }
}
