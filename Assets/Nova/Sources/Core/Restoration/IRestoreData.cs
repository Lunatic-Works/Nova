namespace Nova
{
    // A interface to replace the SerializableAttribute
    // Anything implements this interface are allowed to appear in the dynamic serialized objects of save data (in JSON)
    // When serializing/deserializing, a type check will be done to make sure JSON serialization works
    public interface ISerializedData { }

    /// <summary>
    /// The <b>essential</b> data for the restoration of a restorable object
    /// </summary>
    /// <remarks>
    /// It should be noticed that implementations of this interface should contain only the essential data. It is not
    /// appreciated to include useless utility data in this object, neither to hold any reference to assets.
    /// All the data will be stored in the game state object.
    /// Though not necessary, it is recommended to design the implementation of this interface as immutable.
    /// </remarks>
    public interface IRestoreData : ISerializedData { }
}
