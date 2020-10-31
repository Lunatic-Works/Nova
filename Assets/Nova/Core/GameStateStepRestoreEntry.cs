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
    /// To implement back step functionality, the game state object should know all the GameStateStepRestoreEntry at
    /// each step to perform a back step. To make the back step functionality still work after loading from
    /// a checkpoint, the CheckpointManager should store all the GameStateStepRestoreEntry for walked passed dialogues.
    /// </remarks>
    [Serializable]
    public abstract class GameStateStepRestoreEntry
    {
        /// <summary>
        /// this value is guaranteed to be non-negative.
        /// if StepNumFromLastCheckpoint == 0, restoreDatas is valid
        /// otherwise restoreDatas == null
        /// </summary>
        public readonly int stepNumFromLastCheckpoint;

        /// <summary>
        /// the steps to restrain checkpoints should also be saved
        /// </summary>
        public readonly int restrainCheckpointNum;

        protected GameStateStepRestoreEntry(int stepNumFromLastCheckpoint, int restrainCheckpointNum)
        {
            this.stepNumFromLastCheckpoint = stepNumFromLastCheckpoint;
            this.restrainCheckpointNum = restrainCheckpointNum;
        }
    }

    [Serializable]
    public class GameStateStepRestoreCheckpointEntry : GameStateStepRestoreEntry
    {
        private readonly Dictionary<string, IRestoreData> restoreDatas;

        public readonly Variables variables;

        public GameStateStepRestoreCheckpointEntry(Dictionary<string, IRestoreData> restoreDatas, Variables variables,
            int restrainCheckpointNum)
            : base(0, restrainCheckpointNum)
        {
            this.restoreDatas = restoreDatas;
            this.variables = new Variables();
            this.variables.CopyFrom(variables);
        }

        public IRestoreData this[string restorableObjectName] => restoreDatas[restorableObjectName];
    }

    [Serializable]
    public class GameStateStepRestoreSimpleEntry : GameStateStepRestoreEntry
    {
        public readonly string lastCheckpointVariablesHash;

        public GameStateStepRestoreSimpleEntry(int stepNumFromLastCheckpoint, int restrainCheckpointNum,
            string lastCheckpointVariablesHash)
            : base(stepNumFromLastCheckpoint, restrainCheckpointNum)
        {
            this.lastCheckpointVariablesHash = lastCheckpointVariablesHash;
        }
    }
}