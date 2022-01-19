using UnityEngine;

namespace Nova
{
    public class DebugJumpHelper : MonoBehaviour
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

            if (inputMapper.GetKeyUp(AbstractKey.EditorBackward))
            {
                MoveBackward();
            }

            if (inputMapper.GetKeyUp(AbstractKey.EditorBeginChapter))
            {
                JumpChapter(0);
            }

            if (inputMapper.GetKeyUp(AbstractKey.EditorPreviousChapter))
            {
                JumpChapter(-1);
            }

            if (inputMapper.GetKeyUp(AbstractKey.EditorNextChapter))
            {
                JumpChapter(1);
            }
        }

        private void MoveBackward()
        {
            NovaAnimation.StopAll();
            dialogueBoxController.state = DialogueBoxState.Normal;

            gameState.SeekBackStep(1, out var nodeName, out var dialogueIndex);
            gameState.MoveBackTo(nodeName, dialogueIndex);
        }

        private void JumpChapter(int offset)
        {
            var chapters = gameState.GetAllStartNodeNames();
            int targetChapterIndex = chapters.IndexOf(gameState.currentNode.name) + offset;
            if (targetChapterIndex >= 0 && targetChapterIndex < chapters.Count)
            {
                NovaAnimation.StopAll();
                dialogueBoxController.state = DialogueBoxState.Normal;

                gameState.ResetGameState();
                gameState.GameStart(chapters[targetChapterIndex]);
            }
            else
            {
                Debug.LogWarning($"Nova: No chapter index {targetChapterIndex}");
            }
        }
    }
}