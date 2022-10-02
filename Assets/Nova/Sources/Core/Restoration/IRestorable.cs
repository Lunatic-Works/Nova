namespace Nova
{
    /// <summary>
    /// This interface represents an object that restores its state when GameState moves back
    /// </summary>
    /// <remarks>
    /// The object should register itself to GameState so that its state can be recorded when the dialogue changes
    /// </remarks>
    public interface IRestorable
    {
        /// <summary>
        /// Each IRestorable should have a unique name. This name should not change during the runtime,
        /// and should not be null
        /// </summary>
        string restorableName { get; }

        /// <summary>
        /// Get the data essential for the restoration of the object state
        /// </summary>
        /// <remarks>
        /// This method will be called each time when GameState.dialogueChanged is invoked
        /// </remarks>
        /// <returns>The data essential for the restoration of the object state</returns>
        IRestoreData GetRestoreData();

        /// <summary>
        /// Restore the object state using the restore data
        /// </summary>
        /// <remarks>
        /// This method will be called in Gamestate.MoveBackTo
        /// </remarks>
        /// <param name="restoreData">The data used for the restoration of object state</param>
        void Restore(IRestoreData restoreData);
    }
}
