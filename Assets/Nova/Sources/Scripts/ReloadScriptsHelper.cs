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
            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            dialogueState = controller.DialogueState;
            inputManager = controller.InputManager;
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
        }

        private void ReloadScripts()
        {
            NovaAnimation.StopAll();
            dialogueState.state = DialogueState.State.Normal;
            gameState.ReloadScripts();
            gameState.MoveBackToFirstDialogue();
        }
    }
}
