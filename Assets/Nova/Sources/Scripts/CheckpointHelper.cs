using UnityEngine;

namespace Nova
{
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

        public int warningStepNumFromLastCheckpoint => gameState.warningStepNumFromLastCheckpoint;

        public void RestrainCheckpoint(int steps, bool authorized = false)
        {
            gameState.RestrainCheckpoint(steps, authorized);
        }

        public void EnsureCheckpointOnNextDialogue()
        {
            gameState.EnsureCheckpointOnNextDialogue();
        }

        public void UpdateGlobalSave()
        {
            checkpointManager.UpdateGlobalSave();
        }
    }
}