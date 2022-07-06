using UnityEngine;

namespace Nova
{
    public class ReloadScriptsHelper : MonoBehaviour
    {
        private GameState gameState;
        private DialogueState dialogueState;
        private InputMapper inputMapper;
        private ViewManager viewManager;

        private void Awake()
        {
            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            dialogueState = controller.DialogueState;
            inputMapper = controller.InputMapper;
            viewManager = Utils.FindViewManager();
        }

        private void Update()
        {
            if (viewManager.currentView != CurrentViewType.Game)
            {
                return;
            }

            if (inputMapper.GetKeyUp(AbstractKey.EditorReloadScripts))
            {
                ReloadScripts();
            }

            if (inputMapper.GetKeyUp(AbstractKey.EditorRerunAction))
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