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
                    return Utils.LinearToLogVolume(audioSource.volume);
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
                    audioSource.volume = Utils.LogToLinearVolume(value);
                }
                else
                {
                    audioController.scriptVolume = value;
                }
            }
        }

        public VolumeAnimationProperty(UnifiedAudioSource audioSource, float startValue, float targetValue) :
            base(nameof(VolumeAnimationProperty) + ":" + audioSource, startValue, targetValue)
        {
            this.audioSource = audioSource;
        }

        public VolumeAnimationProperty(UnifiedAudioSource audioSource, float targetValue) :
            base(nameof(VolumeAnimationProperty) + ":" + audioSource, targetValue)
        {
            this.audioSource = audioSource;
        }

        public VolumeAnimationProperty(UnifiedAudioSource audioSource, float deltaValue,
            UseRelativeValue useRelativeValue) :
            base(nameof(VolumeAnimationProperty) + ":" + audioSource, deltaValue, useRelativeValue)
        {
            this.audioSource = audioSource;
        }

        public VolumeAnimationProperty(AudioController audioController, float startValue, float targetValue) :
            base(nameof(VolumeAnimationProperty) + ":" + Utils.GetPath(audioController), startValue, targetValue)
        {
            this.audioController = audioController;
        }

        public VolumeAnimationProperty(AudioController audioController, float targetValue) :
            base(nameof(VolumeAnimationProperty) + ":" + Utils.GetPath(audioController), targetValue)
        {
            this.audioController = audioController;
        }

        public VolumeAnimationProperty(AudioController audioController, float deltaValue,
            UseRelativeValue useRelativeValue) :
            base(nameof(VolumeAnimationProperty) + ":" + Utils.GetPath(audioController), deltaValue, useRelativeValue)
        {
            this.audioController = audioController;
        }
    }
}
