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
            var controller = Utils.FindNovaController();
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
        }

        private void ReloadScripts()
        {
            NovaAnimation.StopAll();
            dialogueState.state = DialogueState.State.Normal;
            gameState.ReloadScripts();
        }
    }
}
