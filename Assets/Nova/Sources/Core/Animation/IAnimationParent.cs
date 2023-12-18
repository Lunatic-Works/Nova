namespace Nova
{
    public interface IAnimationParent
    {
        float totalDuration { get; }
        float totalTimeRemaining { get; }

        AnimationEntry Then(IAnimationProperty property, float duration = 0.0f,
            AnimationEntry.EasingFunction easing = null, int repeatNum = 0);
    }
}
