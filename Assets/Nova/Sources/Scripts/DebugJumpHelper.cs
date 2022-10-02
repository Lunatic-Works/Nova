using UnityEngine;

namespace Nova
{
    public class DebugJumpHelper : MonoBehaviour
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
            dialogueState.state = DialogueState.State.Normal;

            gameState.SeekBackStep(1, out var nodeRecord, out var checkpointOffset, out var dialogueIndex);
            gameState.MoveBackTo(nodeRecord, checkpointOffset, dialogueIndex);
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
                Debug.LogWarning($"Nova: Chapter index {targetChapterIndex} out of range.");
            }
        }
    }
}
