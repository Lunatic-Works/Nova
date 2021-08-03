using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    using GlobalVariables = Dictionary<string, VariableEntry>;

    /// <summary>
    /// Trivial checkpoint management helper for Nova scripts
    /// </summary>
    [ExportCustomType]
    public class CheckpointHelper : MonoBehaviour
    {
        private GameState gameState;
        private CheckpointManager checkpointManager;

        private void Awake()
        {
            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;

            LuaRuntime.Instance.BindObject("checkpointHelper", this);
        }

        public static int WarningStepNumFromLastCheckpoint => GameState.WarningStepNumFromLastCheckpoint;

        public void RestrainCheckpoint(int steps, bool overridden = false)
        {
            gameState.RestrainCheckpoint(steps, overridden);
        }

        public void EnsureCheckpointOnNextDialogue()
        {
            gameState.EnsureCheckpointOnNextDialogue();
        }

        public void UpdateGlobalSave()
        {
            checkpointManager.UpdateGlobalSave();
        }

        #region Global variables

        private const string GlobalVariablesKey = "global_variables";

        private GlobalVariables globalVariables;

        public VariableEntry GetGlobalVariable(string name)
        {
            if (globalVariables == null)
            {
                globalVariables = checkpointManager.Get(GlobalVariablesKey, new GlobalVariables());
            }

            globalVariables.TryGetValue(name, out var entry);
            return entry;
        }

        public void SetGlobalVariable(string name, VariableType type, string value)
        {
            globalVariables[name] = new VariableEntry(type, value);
            checkpointManager.Set(GlobalVariablesKey, globalVariables);
        }

        #endregion
    }
}