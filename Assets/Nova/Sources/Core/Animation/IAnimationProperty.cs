namespace Nova
{
    /// <summary>
    /// The interface for properties to be controlled by NovaAnimation.
    /// </summary>
    public interface IAnimationProperty
    {
        /// <summary>
        /// Name of the property.
        /// </summary>
        string id { get; }

        /// <summary>
        /// The position to interpolate between start and target values, ranging in [0, 1].
        /// </summary>
        float value { get; set; }
    }
}