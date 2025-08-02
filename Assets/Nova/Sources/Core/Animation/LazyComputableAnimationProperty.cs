namespace Nova
{
    // We cannot put this class into LazyComputableAnimationProperty,
    // otherwise the type parameters will cause trouble in Lua binding
    [ExportCustomType]
    public class UseRelativeValue
    {
        public static readonly UseRelativeValue Yes = new UseRelativeValue();
    }

    public abstract class LazyComputableAnimationProperty<T, D> : AnimationProperty
    {
        private bool startValueHasSet;
        private bool targetValueHasSet;
        private T _startValue;
        private T _targetValue;
        private readonly D deltaValue;

        protected abstract T currentValue { get; set; }
        protected abstract T CombineDelta(T a, D b);
        protected abstract T Lerp(T a, T b, float t);

        private void EnsureValuesInitialized()
        {
            if (!startValueHasSet)
            {
                _startValue = currentValue;
                startValueHasSet = true;
            }

            if (!targetValueHasSet)
            {
                _targetValue = CombineDelta(_startValue, deltaValue);
                targetValueHasSet = true;
            }
        }

        private T startValue
        {
            get
            {
                EnsureValuesInitialized();
                return _startValue;
            }
            set
            {
                _startValue = value;
                startValueHasSet = true;
            }
        }

        private T targetValue
        {
            get
            {
                EnsureValuesInitialized();
                return _targetValue;
            }
            set
            {
                _targetValue = value;
                targetValueHasSet = true;
            }
        }

        protected LazyComputableAnimationProperty(string key, T startValue, T targetValue) : base(key)
        {
            this.startValue = startValue;
            this.targetValue = targetValue;
        }

        protected LazyComputableAnimationProperty(string key, T targetValue) : base(key)
        {
            this.targetValue = targetValue;
        }

        protected LazyComputableAnimationProperty(string key, D deltaValue, UseRelativeValue useRelativeValue) : base(key)
        {
            this.deltaValue = deltaValue;
        }

        private float _value;

        public override float value
        {
            get => _value;
            set
            {
                // The lock will be released in AnimationProperty.Dispose()
                AcquireLock();
                _value = value;
                currentValue = Lerp(startValue, targetValue, value);
            }
        }
    }
}
