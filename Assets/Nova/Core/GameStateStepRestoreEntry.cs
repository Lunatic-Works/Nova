using System.Collections.Generic;

namespace Nova
{
    /// <summary>
    /// This class stores all restoration infomation for all registered restorable under a game object at a
    /// certain step
    /// </summary>
    /// <remarks>
    /// Due to the design of the Nova script syntax, the status of objects at each step can only be known at runtime.
    /// To implement back step functionality, the game state object should know all the GameStateStepRestoreEntry at
    /// each step to perfrom a back step. To make the back step functionality still work after loading from
    /// a checkpoint, the CheckpointManager should store all the GameStateStepRestoreEntry for walked passed dialogues.
    /// </remarks>
    public class GameStateStepRestoreEntry
    {
        private readonly Dictionary<string, IRestoreData> restoreDatas;

        public GameStateStepRestoreEntry(Dictionary<string, IRestoreData> restoreDatas)
        {
            this.restoreDatas = restoreDatas;
        }

        public IRestoreData this[string restorableObjectName]
        {
            get { return restoreDatas[restorableObjectName]; }
        }
    }
}