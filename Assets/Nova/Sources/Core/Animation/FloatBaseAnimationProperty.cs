using UnityEngine;

namespace Nova
{
    public abstract class FloatBaseAnimationProperty : LazyComputableAnimationProperty<float, float>
    {
        protected override float CombineDelta(float a, float b) => a + b;

        protected override float Lerp(float a, float b, float t) => Mathf.LerpUnclamped(a, b, t);

        protected FloatBaseAnimationProperty(string key, float startValue, float targetValue) :
            base(key, startValue, targetValue) { }

        protected FloatBaseAnimationProperty(string key, float targetValue) :
            base(key, targetValue) { }

        protected FloatBaseAnimationProperty(string key, float deltaValue, UseRelativeValue useRelativeValue) :
            base(key, deltaValue, useRelativeValue) { }
    }
}
