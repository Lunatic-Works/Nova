using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class TimeAnimationProperty : FloatBaseAnimationProperty
    {
        private readonly TimelineController timeline;

        protected override float currentValue
        {
            get
            {
                var playableDirector = timeline.playableDirector;
                if (playableDirector == null)
                {
                    return 0.0f;
                }

                return (float)playableDirector.time;
            }
            set
            {
                var playableDirector = timeline.playableDirector;
                if (playableDirector == null)
                {
                    return;
                }

                playableDirector.time = value;
                playableDirector.Evaluate();
            }
        }

        protected override float CombineDelta(float a, float b) =>
            Mathf.Clamp(a + b, 0.0f, (float)timeline.playableDirector.duration);

        public TimeAnimationProperty(TimelineController timeline, float startValue, float targetValue) : base(
            startValue, targetValue)
        {
            this.timeline = timeline;
        }

        public TimeAnimationProperty(TimelineController timeline, float targetValue) : base(targetValue)
        {
            this.timeline = timeline;
        }

        public TimeAnimationProperty(TimelineController timeline, float deltaValue, UseRelativeValue useRelativeValue) :
            base(deltaValue, useRelativeValue)
        {
            this.timeline = timeline;
        }
    }
}
