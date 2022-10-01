using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class RotationAnimationProperty : LazyComputableAnimationProperty<Quaternion, Quaternion>
    {
        private readonly Transform transform;

        protected override Quaternion currentValue
        {
            get => transform.localRotation;
            set => transform.localRotation = value;
        }

        protected override Quaternion CombineDelta(Quaternion a, Quaternion b) => a * b;

        protected override Quaternion Lerp(Quaternion a, Quaternion b, float t) => Quaternion.SlerpUnclamped(a, b, t);

        public RotationAnimationProperty(Transform transform, Vector3 startEuler, Vector3 targetEuler)
            : base(Quaternion.Euler(startEuler), Quaternion.Euler(targetEuler))
        {
            this.transform = transform;
        }

        public RotationAnimationProperty(Transform transform, Vector3 targetEuler) : base(Quaternion.Euler(targetEuler))
        {
            this.transform = transform;
        }

        public RotationAnimationProperty(Transform transform, Vector3 deltaEuler, UseRelativeValue useRelativeValue) :
            base(Quaternion.Euler(deltaEuler), useRelativeValue)
        {
            this.transform = transform;
        }
    }
}
