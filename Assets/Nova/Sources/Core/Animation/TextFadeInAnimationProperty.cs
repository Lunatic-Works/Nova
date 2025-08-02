namespace Nova
{
    [ExportCustomType]
    public class TextFadeInAnimationProperty : AnimationProperty
    {
        private readonly TextProxy text;

        public TextFadeInAnimationProperty(TextProxy text) :
            base(nameof(TextFadeInAnimationProperty) + ":" + Utils.GetPath(text))
        {
            this.text = text;
            // Avoid undesired flash on the first frame
            text.SetFade(0f);
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
                text.SetFade(value);
            }
        }
    }
}
