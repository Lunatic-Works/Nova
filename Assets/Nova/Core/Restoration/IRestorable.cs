namespace Nova
{
    /// <summary>
    /// This interface represents an object that can restore its state when the GameState goes back
    /// </summary>
    /// <remarks>
    /// The object should manually register itself to the GameState so that its state can be recorded and stored when
    /// the dialogue changes
    /// </remarks>
    public interface IRestorable
    {
        /// <summary>
        /// All IRestorable should have a unique name, this name should not change during the runtime,
        /// and should not be null
        /// </summary>
        string restorableObjectName { get; }

        /// <summary>
        /// Get the data essential for the restoration of the game object state
        /// </summary>
        /// <remarks>
        /// This method will be called each time after DialogueChanged event happens
        /// </remarks>
        /// <returns>The data essential for the restoration of the game object state</returns>
        IRestoreData GetRestoreData();

        /// <summary>
        /// Restore the restorable object using the restore data
        /// </summary>
        /// <remarks>
        /// This method will be called when the game flow goes backwards
        /// </remarks>
        /// <param name="restoreData">the data used for the restoration of game object state</param>
        void Restore(IRestoreData restoreData);
    }
}