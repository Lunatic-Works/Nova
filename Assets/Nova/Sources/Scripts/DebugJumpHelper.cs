using UnityEngine;

namespace Nova
{
    public class DebugJumpHelper : MonoBehaviour
    {
        private GameState gameState;
        private DialogueState dialogueState;
        private InputManager inputManager;
        private ViewManager viewManager;

        private void Awake()
        {
            var controller = Utils.FindNovaGameController();
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

            if (inputManager.IsTriggered(AbstractKey.EditorBackward))
            {
                MoveBackward();
            }

            if (inputManager.IsTriggered(AbstractKey.EditorBeginChapter))
            {
                JumpChapter(0);
            }

            if (inputManager.IsTriggered(AbstractKey.EditorPreviousChapter))
            {
                JumpChapter(-1);
            }

            if (inputManager.IsTriggered(AbstractKey.EditorNextChapter))
            {
                JumpChapter(1);
            }
        }

        private void MoveBackward()
        {
            NovaAnimation.StopAll();
            dialogueState.state = DialogueState.State.Normal;

            if (gameState.SeekBackStep(1, out var nodeName, out var dialogueIndex))
            {
                gameState.MoveBackTo(nodeName, dialogueIndex);
            }
            else
            {
                Debug.LogWarning("Nova: Cannot move backward at the first dialogue.");
            }
        }

        private void JumpChapter(int offset)
        {
            NovaAnimation.StopAll();
            dialogueState.state = DialogueState.State.Normal;

            var chapters = gameState.GetAllStartNodeNames();
            int targetChapterIndex = chapters.IndexOf(gameState.currentNode.name) + offset;
            if (targetChapterIndex >= 0 && targetChapterIndex < chapters.Count)
            {
                gameState.GameStart(chapters[targetChapterIndex]);
            }
            else
            {
                Debug.LogWarning($"Nova: No chapter index {targetChapterIndex}");
            }
        }
    }
}