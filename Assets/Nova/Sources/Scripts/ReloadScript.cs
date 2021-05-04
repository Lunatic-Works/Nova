using UnityEngine;

namespace Nova
{
    public class ReloadScript : MonoBehaviour
    {
        [SerializeField] private GameObject characters;
        [SerializeField] private SoundController soundController;
        [SerializeField] private ViewManager viewManager;

        private GameState gameState;
        private InputMapper inputMapper;
        private CharacterController[] characterControllers;
        private string currentNodeInitialVariablesHash;

        private void Start()
        {
            var gameController = Utils.FindNovaGameController();
            gameState = gameController.GameState;
            inputMapper = gameController.InputMapper;

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

        private void OnNodeChanged(NodeChangedData nodeChangedData)
        {
            Debug.Log($"ReloadScript OnNodeChanged {nodeChangedData.nodeName} {gameState.variables.hash}");
            currentNodeInitialVariablesHash = gameState.variables.hash;
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

            if (inputMapper.GetKeyUp(AbstractKey.EditorRerunNode))
            {
                RerunNode();
            }

            if (inputMapper.GetKeyUp(AbstractKey.EditorRerunAction))
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

        private void ReloadScripts()
        {
            if (!gameState) return;
            NovaAnimation.StopAll();
            LuaRuntime.Instance.InitRequires();
            gameState.ReloadScripts();
        }

        private void RerunNode()
        {
            if (!gameState) return;
            NovaAnimation.StopAll();
            var currentNode = gameState.currentNode;
            var currentIndex = gameState.currentIndex;
            SuppressSound(true);
            Debug.Log($"MoveBackTo {currentNode.name} {currentNodeInitialVariablesHash}");
            gameState.MoveBackTo(currentNode.name, 0, currentNodeInitialVariablesHash, clearFuture: true);
            LuaRuntime.Instance.InitRequires();
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

        private void RerunAction()
        {
            if (!gameState) return;
            gameState.currentNode.GetDialogueEntryAt(gameState.currentIndex).ExecuteAction();
        }
    }
}