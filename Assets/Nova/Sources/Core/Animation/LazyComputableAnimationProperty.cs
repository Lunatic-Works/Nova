namespace Nova
{
    // We cannot put this class into LazyComputableAnimationProperty,
    // otherwise the type parameters will cause trouble in Lua binding
    [ExportCustomType]
    public class UseRelativeValue
    {
        public static readonly UseRelativeValue Yes = new UseRelativeValue();
    }

    public abstract class LazyComputableAnimationProperty<T, D> : IAnimationProperty
    {
        private bool startValueHasSet = false;
        private bool targetValueHasSet = false;
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

        protected LazyComputableAnimationProperty(T startValue, T targetValue)
        {
            this.startValue = startValue;
            this.targetValue = targetValue;
        }

        protected LazyComputableAnimationProperty(T targetValue)
        {
            this.targetValue = targetValue;
        }

        protected LazyComputableAnimationProperty(D deltaValue, UseRelativeValue useRelativeValue)
        {
            this.deltaValue = deltaValue;
        }

        private float _value;

        public float value
        {
            get => _value;
            set
            {
                _value = value;
                currentValue = Lerp(startValue, targetValue, value);
            }
        }
    }
}
