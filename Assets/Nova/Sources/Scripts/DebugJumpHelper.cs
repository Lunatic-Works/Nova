using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    public class DebugJumpHelper : MonoBehaviour
    {
        public bool backward;
        public bool previousChapter, nextChapter;
        public bool previousBranch, nextBranch;

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

            if (backward)
            {
                backward = false;
                MoveBackward();
            }

            if (previousChapter)
            {
                previousChapter = false;
                gameState.MoveToKeyPoint(false, true);
            }

            if (nextChapter)
            {
                nextChapter = false;
                gameState.MoveToKeyPoint(true, true);
            }

            if (previousBranch)
            {
                previousBranch = false;
                gameState.MoveToKeyPoint(false, false);
            }

            if (nextBranch)
            {
                nextBranch = false;
                gameState.MoveToKeyPoint(true, false);
            }
        }

        private void MoveBackward()
        {
            NovaAnimation.StopAll();
            dialogueState.state = DialogueState.State.Normal;

            gameState.SeekBackStep(1, out var nodeRecord, out var checkpointOffset, out var dialogueIndex);
            gameState.MoveBackTo(nodeRecord, checkpointOffset, dialogueIndex);
        }
    }
}
