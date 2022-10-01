using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class TimeAnimationProperty : FloatBaseAnimationProperty
    {
        private readonly TimelineController timeline;

        protected override float currentValue
        {
            get => (float)timeline.playableDirector.time;
            set
            {
                timeline.playableDirector.time = value;
                timeline.playableDirector.Evaluate();
            }
        }

        protected override float CombineDelta(float a, float b) =>
            Mathf.Clamp(a + b, 0f, (float)timeline.playableDirector.duration);

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
