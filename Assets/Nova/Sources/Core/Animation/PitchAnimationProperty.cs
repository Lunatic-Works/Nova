namespace Nova
{
    [ExportCustomType]
    public class PitchAnimationProperty : FloatBaseAnimationProperty
    {
        private readonly UnifiedAudioSource audioSource;
        private readonly AudioController audioController;

        protected override float currentValue
        {
            get
            {
                if (audioSource != null)
                {
                    return audioSource.pitch;
                }
                else
                {
                    return audioController.pitch;
                }
            }
            set
            {
                if (audioSource != null)
                {
                    audioSource.pitch = value;
                }
                else
                {
                    audioController.pitch = value;
                }
            }
        }

        public PitchAnimationProperty(UnifiedAudioSource audioSource, float startValue, float targetValue) : base(
            startValue, targetValue)
        {
            this.audioSource = audioSource;
        }

        public PitchAnimationProperty(UnifiedAudioSource audioSource, float targetValue) : base(targetValue)
        {
            this.audioSource = audioSource;
        }

        public PitchAnimationProperty(UnifiedAudioSource audioSource, float deltaValue,
            UseRelativeValue useRelativeValue) : base(deltaValue, useRelativeValue)
        {
            this.audioSource = audioSource;
        }

        public PitchAnimationProperty(AudioController audioController, float startValue, float targetValue) : base(
            startValue, targetValue)
        {
            this.audioController = audioController;
        }

        public PitchAnimationProperty(AudioController audioController, float targetValue) : base(targetValue)
        {
            this.audioController = audioController;
        }

        public PitchAnimationProperty(AudioController audioController, float deltaValue,
            UseRelativeValue useRelativeValue) : base(deltaValue, useRelativeValue)
        {
            this.audioController = audioController;
        }
    }
}
