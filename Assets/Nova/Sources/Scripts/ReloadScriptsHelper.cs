using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class ReloadScriptsHelper : MonoBehaviour
    {
        private GameState gameState;
        private InputMapper inputMapper;
        private ViewManager viewManager;
        private DialogueBoxController dialogueBoxController;

        private void Awake()
        {
            var gameController = Utils.FindNovaGameController();
            gameState = gameController.GameState;
            inputMapper = gameController.InputMapper;
            viewManager = Utils.FindViewManager();
            dialogueBoxController = viewManager.GetController<DialogueBoxController>();
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
            dialogueBoxController.state = DialogueBoxState.Normal;
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