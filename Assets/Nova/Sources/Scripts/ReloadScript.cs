using UnityEngine;

namespace Nova
{
    public class ReloadScript : MonoBehaviour
    {
        [SerializeField] private GameObject characters;
        [SerializeField] private SoundController soundController;
        [SerializeField] private ViewManager viewManager;

        private GameState gameState;
        private CharacterController[] characterControllers;
        private string currentNodeInitialVariablesHash;

        private void Start()
        {
            gameState = Utils.FindNovaGameController().GameState;

            if (characters != null)
            {
                characterControllers = characters.GetComponentsInChildren<CharacterController>();
            }

            if (!Application.isEditor) return;

            currentNodeInitialVariablesHash = "";
            gameState.NodeChanged += OnNodeChanged;
        }

        private void OnDestroy()
        {
            gameState.NodeChanged -= OnNodeChanged;
        }

        private void OnNodeChanged(NodeChangedData arg0)
        {
            currentNodeInitialVariablesHash = gameState.variables.hash;
        }

        private void Update()
        {
            if (viewManager.currentView != CurrentViewType.Game)
            {
                return;
            }

            if (Utils.GetKeyDownInEditor(KeyCode.R))
            {
                if (Utils.GetKeyInEditor(KeyCode.LeftShift))
                {
                    ReloadScriptOnly();
                }
                else
                {
                    ReloadAndRefreshNode();
                }
            }

            if (Utils.GetKeyDownInEditor(KeyCode.F))
            {
                RerunAction();
            }
        }

        private void SuppressSound(bool v)
        {
            if (characterControllers != null)
            {
                foreach (var characterController in characterControllers)
                {
                    characterController.suppressSound = v;
                }
            }

            if (soundController != null)
            {
                soundController.suppressSound = v;
            }
        }

        private void ReloadAndRefreshNode()
        {
            if (!gameState) return;
            NovaAnimation.StopAll();
            var currentNode = gameState.currentNode;
            var currentIndex = gameState.currentIndex;
            SuppressSound(true);
            gameState.MoveBackTo(currentNode.name, 0, currentNodeInitialVariablesHash, clearFuture: true);
            gameState.ReloadScripts();

            // step back to current index
            for (var i = 0; i < currentIndex - 1; i++)
            {
                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                gameState.Step();
            }

            NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
            SuppressSound(false); // only the last step can play sound
            gameState.Step();
        }

        private void ReloadScriptOnly()
        {
            if (!gameState) return;
            NovaAnimation.StopAll();
            gameState.ReloadScripts();
        }

        private void RerunAction()
        {
            if (!gameState) return;
            gameState.currentNode.GetDialogueEntryAt(gameState.currentIndex).ExecuteAction();
        }
    }
}