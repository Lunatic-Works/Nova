namespace Nova
{
    [ExportCustomType]
    public class VolumeAnimationProperty : FloatBaseAnimationProperty
    {
        private readonly UnifiedAudioSource audioSource;
        private readonly AudioController audioController;

        protected override float currentValue
        {
            get
            {
                if (audioSource != null)
                {
                    return audioSource.volume;
                }
                else
                {
                    return audioController.scriptVolume;
                }
            }
            set
            {
                if (audioSource != null)
                {
                    audioSource.volume = value;
                }
                else
                {
                    audioController.scriptVolume = value;
                }
            }
        }

        public VolumeAnimationProperty(UnifiedAudioSource audioSource, float startValue, float targetValue) : base(
            startValue, targetValue)
        {
            this.audioSource = audioSource;
        }

        public VolumeAnimationProperty(UnifiedAudioSource audioSource, float targetValue) : base(targetValue)
        {
            this.audioSource = audioSource;
        }

        public VolumeAnimationProperty(UnifiedAudioSource audioSource, float deltaValue,
            UseRelativeValue useRelativeValue) : base(deltaValue, useRelativeValue)
        {
            this.audioSource = audioSource;
        }

        public VolumeAnimationProperty(AudioController audioController, float startValue, float targetValue) : base(
            startValue, targetValue)
        {
            this.audioController = audioController;
        }

        public VolumeAnimationProperty(AudioController audioController, float targetValue) : base(targetValue)
        {
            this.audioController = audioController;
        }

        public VolumeAnimationProperty(AudioController audioController, float deltaValue,
            UseRelativeValue useRelativeValue) : base(deltaValue, useRelativeValue)
        {
            this.audioController = audioController;
        }
    }
}
