using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class DebugJumpHelper : MonoBehaviour
    {
        [SerializeField] private bool moveBackward;
        [SerializeField] private bool previousChapter;
        [SerializeField] private bool nextChapter;
        [SerializeField] private bool previousBranch;
        [SerializeField] private bool nextBranch;

        private GameState gameState;
        private DialogueState dialogueState;
        private ViewManager viewManager;

        private void Awake()
        {
            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            dialogueState = controller.DialogueState;
            viewManager = Utils.FindViewManager();
        }

        private void Update()
        {
            if (viewManager.currentView != CurrentViewType.Game)
            {
                return;
            }

            if (moveBackward)
            {
                moveBackward = false;
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
