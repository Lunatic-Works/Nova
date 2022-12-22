using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class DebugJumpHelper : MonoBehaviour
    {
        private GameState gameState;
        private DialogueState dialogueState;
        private InputManager inputManager;
        private ViewManager viewManager;

        private IReadOnlyList<string> _chapters;

        private IReadOnlyList<string> chapters => _chapters ??= gameState.GetStartNodeNames().ToList();

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

            gameState.SeekBackStep(1, out var nodeRecord, out var checkpointOffset, out var dialogueIndex);
            gameState.MoveBackTo(nodeRecord, checkpointOffset, dialogueIndex);
        }

        private void JumpChapter(int offset)
        {
            NovaAnimation.StopAll();
            dialogueState.state = DialogueState.State.Normal;

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
