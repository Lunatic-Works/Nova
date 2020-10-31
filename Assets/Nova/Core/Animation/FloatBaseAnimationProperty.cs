using UnityEngine;

namespace Nova
{
    public abstract class FloatBaseAnimationProperty : LazyComputableAnimationProperty<float, float>
    {
        protected FloatBaseAnimationProperty(float targetValue) : base(targetValue) { }

        protected FloatBaseAnimationProperty(float deltaValue, UseRelativeValue useRelativeValue) : base(deltaValue,
            useRelativeValue) { }

        protected FloatBaseAnimationProperty(float startValue, float targetValue) : base(startValue, targetValue) { }

        protected override float Lerp(float a, float b, float t) => Mathf.LerpUnclamped(a, b, t);

        protected override float InverseLerp(float a, float b, float curr) => Mathf.InverseLerp(a, b, curr);
    }
}