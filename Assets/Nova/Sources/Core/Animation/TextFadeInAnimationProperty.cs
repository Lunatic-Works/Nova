namespace Nova
{
    [ExportCustomType]
    public class TextFadeInAnimationProperty : IAnimationProperty
    {
        private readonly TextProxy text;

        public TextFadeInAnimationProperty(TextProxy text)
        {
            this.text = text;
            // Avoid undesired flash on the first frame
            value = 0f;
        }

        private float _value;

        public float value
        {
            get => _value;
            set
            {
                _value = value;
                text.SetFade(value);
            }
        }
    }
}
