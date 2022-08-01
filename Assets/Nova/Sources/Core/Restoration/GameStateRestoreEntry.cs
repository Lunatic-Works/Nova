using System;
using System.Collections.Generic;

namespace Nova
{
    /// <summary>
    /// This class stores all restoration information for all registered restorable under a game object at a
    /// certain step
    /// </summary>
    /// <remarks>
    /// Due to the design of the Nova script syntax, the status of objects at each step can only be known at runtime.
    /// To implement back step functionality, the game state object should know all the GameStateRestoreEntry at
    /// each step to perform a back step. To make the back step functionality still work after loading from
    /// a checkpoint, the CheckpointManager should store all the GameStateRestoreEntry for walked passed dialogues.
    /// </remarks>
    [Serializable]
    public class GameStateCheckpoint
    {
        public readonly int dialogueIndex;
        public readonly int restrainCheckpointNum;
        public readonly IReadOnlyDictionary<string, IRestoreData> restoreDatas;
        public readonly Variables variables;

        public GameStateCheckpoint(int dialogueIndex, IReadOnlyDictionary<string, IRestoreData> restoreDatas,
            Variables variables, int restrainCheckpointNum)
        {
            this.dialogueIndex = dialogueIndex;
            this.restrainCheckpointNum = restrainCheckpointNum;
            this.restoreDatas = restoreDatas;
            this.variables = new Variables();
            this.variables.CloneFrom(variables);
        }
    }
}
