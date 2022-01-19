using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class ReloadScriptsHelper : MonoBehaviour
    {
        [SerializeField] private GameObject characters;
        [SerializeField] private SoundController soundController;

        private GameState gameState;
        private InputMapper inputMapper;
        private ViewManager viewManager;
        private DialogueBoxController dialogueBoxController;

        private List<CharacterController> characterControllers;

        private void Awake()
        {
            var gameController = Utils.FindNovaGameController();
            gameState = gameController.GameState;
            inputMapper = gameController.InputMapper;
            viewManager = Utils.FindViewManager();
            dialogueBoxController = viewManager.GetController<DialogueBoxController>();

            if (characters != null)
            {
                characterControllers = characters.GetComponentsInChildren<CharacterController>().ToList();
            }
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
            NovaAnimation.StopAll();
            dialogueBoxController.state = DialogueBoxState.Normal;

            gameState.ReloadScripts();

            SuppressSound(true);
            var currentIndex = gameState.currentIndex;
            gameState.MoveBackTo(gameState.nodeHistory.GetCounted(gameState.currentNode.name), 0, clearFuture: true);

            // Step back to current index
            for (var i = 0; i < currentIndex; ++i)
            {
                // Only the last step can play sound
                if (i == currentIndex - 1)
                {
                    SuppressSound(false);
                }

                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                gameState.Step();
            }
        }

        private void RerunAction()
        {
            // TODO
            // NovaAnimation.StopAll();
            // dialogueBoxController.state = DialogueBoxState.Normal;
            // 
            // gameState.currentNode.GetDialogueEntryAt(gameState.currentIndex)
            //     .ExecuteAction(DialogueActionStage.Default, false);
        }
    }
}