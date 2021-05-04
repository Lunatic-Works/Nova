using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Trivial checkpoint management helper for Nova scripts
    /// </summary>
    [ExportCustomType]
    public class CheckpointHelper
    {
        private GameState _gameState;

        private GameState gameState
        {
            get
            {
                if (_gameState == null)
                {
                    _gameState = Utils.FindNovaGameController().GameState;
                }

                return _gameState;
            }
        }

        private CheckpointManager _checkpointManager;

        private CheckpointManager checkpointManager
        {
            get
            {
                if (_checkpointManager == null)
                {
                    _checkpointManager = Utils.FindNovaGameController().CheckpointManager;
                }

                return _checkpointManager;
            }
        }

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