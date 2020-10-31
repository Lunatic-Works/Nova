using UnityEngine.UI;

namespace Nova
{
    public class VerticalScrollRectAnimationProperty : FloatBaseAnimationProperty
    {
        private readonly ScrollRect scrollRect;

        public VerticalScrollRectAnimationProperty(ScrollRect scrollRect, float targetValue) : base(targetValue)
        {
            this.scrollRect = scrollRect;
        }

        public override string id => "VerticalScrollRectAnimationProperty";

        protected override float currentValue
        {
            get => scrollRect.verticalNormalizedPosition;
            set => scrollRect.verticalNormalizedPosition = value;
        }

        protected override float CombineDelta(float a, float b) => a + b;
    }
}