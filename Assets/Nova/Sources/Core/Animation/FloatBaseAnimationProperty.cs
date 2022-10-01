using UnityEngine;

namespace Nova
{
    public abstract class FloatBaseAnimationProperty : LazyComputableAnimationProperty<float, float>
    {
        protected override float CombineDelta(float a, float b) => a + b;

        protected override float Lerp(float a, float b, float t) => Mathf.LerpUnclamped(a, b, t);

        protected FloatBaseAnimationProperty(float startValue, float targetValue) : base(startValue, targetValue) { }

        protected FloatBaseAnimationProperty(float targetValue) : base(targetValue) { }

        protected FloatBaseAnimationProperty(float deltaValue, UseRelativeValue useRelativeValue) : base(deltaValue,
            useRelativeValue) { }
    }
}
