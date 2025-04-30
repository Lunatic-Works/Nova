using UnityEngine.UI;

namespace Nova
{
    [ExportCustomType]
    public class VerticalScrollRectAnimationProperty : FloatBaseAnimationProperty
    {
        private readonly ScrollRect scrollRect;

        protected override float currentValue
        {
            get => scrollRect.verticalNormalizedPosition;
            set => scrollRect.verticalNormalizedPosition = value;
        }

        public VerticalScrollRectAnimationProperty(ScrollRect scrollRect, float targetValue) :
            base(nameof(VerticalScrollRectAnimationProperty) + ":" + Utils.GetPath(scrollRect), targetValue)
        {
            this.scrollRect = scrollRect;
        }
    }
}
