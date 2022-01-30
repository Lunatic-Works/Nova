using System;
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

        private void EnsureGlobalVariables()
        {
            if (globalVariables == null)
            {
                globalVariables = checkpointManager.Get(GlobalVariablesKey, new GlobalVariables());
            }
        }

        public VariableEntry GetGlobalVariable(string name)
        {
            EnsureGlobalVariables();

            globalVariables.TryGetValue(name, out var entry);
            return entry;
        }

        public T GetGlobalVariable<T>(string name, T defaultValue = default)
        {
            EnsureGlobalVariables();

            if (globalVariables.TryGetValue(name, out var entry))
            {
                return (T)Convert.ChangeType(entry.value, typeof(T));
            }
            else
            {
                return defaultValue;
            }
        }

        public void SetGlobalVariable(string name, VariableType type, object value)
        {
            EnsureGlobalVariables();

            if (value == null)
            {
                globalVariables.Remove(name);
            }
            else
            {
                globalVariables[name] = new VariableEntry(type, value);
            }

            checkpointManager.Set(GlobalVariablesKey, globalVariables);
        }

        public void SetGlobalVariable<T>(string name, T value)
        {
            var t = typeof(T);
            if (value == null)
            {
                SetGlobalVariable(name, VariableType.String, null);
            }
            else if (t == typeof(bool))
            {
                SetGlobalVariable(name, VariableType.Boolean, value);
            }
            else if (Utils.IsNumericType(t))
            {
                SetGlobalVariable(name, VariableType.Number, value);
            }
            else if (t == typeof(string))
            {
                SetGlobalVariable(name, VariableType.String, value);
            }
            else
            {
                throw new ArgumentException(
                    $"Nova: Variable can only be bool, numeric types, string, or null, but found {t}: {value}");
            }
        }

        #endregion
    }
}