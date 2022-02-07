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
    public abstract class GameStateRestoreEntry
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

        protected GameStateRestoreEntry(int stepNumFromLastCheckpoint, int restrainCheckpointNum)
        {
            this.stepNumFromLastCheckpoint = stepNumFromLastCheckpoint;
            this.restrainCheckpointNum = restrainCheckpointNum;
        }
    }

    [Serializable]
    public class GameStateCheckpoint : GameStateRestoreEntry
    {
        public readonly IReadOnlyDictionary<string, IRestoreData> restoreDatas;
        public readonly Variables variables;

        public GameStateCheckpoint(IReadOnlyDictionary<string, IRestoreData> restoreDatas, Variables variables,
            int restrainCheckpointNum)
            : base(0, restrainCheckpointNum)
        {
            this.restoreDatas = restoreDatas;
            this.variables = new Variables();
            this.variables.CopyFrom(variables);
        }
    }

    [Serializable]
    public class GameStateSimpleEntry : GameStateRestoreEntry
    {
        public GameStateSimpleEntry(int stepNumFromLastCheckpoint, int restrainCheckpointNum)
            : base(stepNumFromLastCheckpoint, restrainCheckpointNum)
        {
            Utils.RuntimeAssert(stepNumFromLastCheckpoint > 0,
                $"Invalid stepNumFromLastCheckpoint for GameStateSimpleEntry: {stepNumFromLastCheckpoint}");
        }
    }
}