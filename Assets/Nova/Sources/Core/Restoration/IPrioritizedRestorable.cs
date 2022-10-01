namespace Nova
{
    public interface IPrioritizedRestorable : IRestorable
    {
        RestorablePriority priority { get; }
    }

    public enum RestorablePriority
    {
        Normal = 0,
        Early = 1,
        Preload = 2
    }
}
