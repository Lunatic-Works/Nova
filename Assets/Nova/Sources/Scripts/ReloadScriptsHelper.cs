using UnityEngine;

namespace Nova
{
    public class ReloadScriptsHelper : MonoBehaviour
    {
        private GameState gameState;
        private DialogueState dialogueState;
        private InputManager inputManager;
        private ViewManager viewManager;

        private void Awake()
        {
            var gameController = Utils.FindNovaGameController();
            gameState = gameController.GameState;
            dialogueState = gameController.DialogueState;
            inputManager = gameController.InputManager;
            viewManager = Utils.FindViewManager();
        }

        private void Update()
        {
            if (viewManager.currentView != CurrentViewType.Game)
            {
                return;
            }

            if (inputManager.IsTriggered(AbstractKey.EditorReloadScripts))
            {
                ReloadScripts();
            }

            if (inputManager.IsTriggered(AbstractKey.EditorRerunAction))
            {
                RerunAction();
            }
        }

        private void ReloadScripts()
        {
            NovaAnimation.StopAll();
            dialogueState.state = DialogueState.State.Normal;
            gameState.ReloadScripts();
            var nodeHistoryEntry = gameState.nodeHistory.GetCounted(gameState.currentNode.name);
            gameState.MoveBackAndFastForward(nodeHistoryEntry, 0, gameState.currentIndex, true, null);
        }

        private void RerunAction()
        {
            // TODO: how is this useful?
            // NovaAnimation.StopAll();
            // dialogueBoxController.state = DialogueBoxState.Normal;
            // gameState.currentNode.GetDialogueEntryAt(gameState.currentIndex)
            //     .ExecuteAction(DialogueActionStage.Default, false);
        }
    }
}