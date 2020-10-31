namespace Nova
{
    /// <summary>
    /// The <b>essential</b> data for the restoration of a restorable object
    /// </summary>
    /// <remarks>
    /// It should be noticed that implementations of this interface should contain only the essential data. It is not
    /// appreciated to include useless utility data in this object, neither to hold any reference to assets.
    /// All the data will be stored in the game state object.
    /// Though not necessary, it is recommended to design the implementation of this interface as immutable.
    /// </remarks>
    public interface IRestoreData { }
}