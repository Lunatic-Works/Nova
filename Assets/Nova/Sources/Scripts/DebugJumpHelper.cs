using UnityEngine;

namespace Nova
{
    public class DebugJumpHelper : MonoBehaviour
    {
        [SerializeField] private bool stepBackward;
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

            if (stepBackward)
            {
                stepBackward = false;
                StepBackward();
            }

            if (previousChapter)
            {
                previousChapter = false;
                gameState.MoveToKeyPoint(false, true);
            }

            if (nextChapter)
            {
                nextChapter = false;
                // gameState.MoveToKeyPoint(true, true);
                gameState.JumpToNextChapter();
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

        private void StepBackward()
        {
            NovaAnimation.StopAll(AnimationType.All ^ AnimationType.UI);
            dialogueState.state = DialogueState.State.Normal;

            gameState.StepBackward();
        }
    }
}
