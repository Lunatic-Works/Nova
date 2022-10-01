namespace Nova
{
    /// <summary>
    /// The interface for properties controlled by NovaAnimation.
    /// </summary>
    public interface IAnimationProperty
    {
        /// <summary>
        /// The parameter to interpolate between the start and the target values, ranging in [0, 1].
        /// </summary>
        float value { get; set; }
    }
}
