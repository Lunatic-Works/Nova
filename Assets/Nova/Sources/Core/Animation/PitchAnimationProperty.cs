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

        public PitchAnimationProperty(UnifiedAudioSource audioSource, float startValue, float targetValue) :
            base(nameof(PitchAnimationProperty) + ":" + audioSource, startValue, targetValue)
        {
            this.audioSource = audioSource;
        }

        public PitchAnimationProperty(UnifiedAudioSource audioSource, float targetValue) :
            base(nameof(PitchAnimationProperty) + ":" + audioSource, targetValue)
        {
            this.audioSource = audioSource;
        }

        public PitchAnimationProperty(UnifiedAudioSource audioSource, float deltaValue,
            UseRelativeValue useRelativeValue) :
            base(nameof(PitchAnimationProperty) + ":" + audioSource, deltaValue, useRelativeValue)
        {
            this.audioSource = audioSource;
        }

        public PitchAnimationProperty(AudioController audioController, float startValue, float targetValue) :
            base(nameof(PitchAnimationProperty) + ":" + Utils.GetPath(audioController), startValue, targetValue)
        {
            this.audioController = audioController;
        }

        public PitchAnimationProperty(AudioController audioController, float targetValue) :
            base(nameof(PitchAnimationProperty) + ":" + Utils.GetPath(audioController), targetValue)
        {
            this.audioController = audioController;
        }

        public PitchAnimationProperty(AudioController audioController, float deltaValue,
            UseRelativeValue useRelativeValue) :
            base(nameof(PitchAnimationProperty) + ":" + Utils.GetPath(audioController), deltaValue, useRelativeValue)
        {
            this.audioController = audioController;
        }
    }
}
