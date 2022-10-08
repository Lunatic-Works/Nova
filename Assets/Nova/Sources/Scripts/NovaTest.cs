using System;
using System.Collections;
using UnityEngine;

namespace Nova
{
    public class NovaTest : MonoBehaviour
    {
        public int steps;
        public bool fastForward;
        public float delaySeconds = 0.8f;
        public int seed;

        private GameState gameState;
        private ViewManager viewManager;
        private DialogueBoxController dialogueBox;
        private SaveViewController saveView;
        private BranchController branchController;
        private HelpViewController helpView;
        private TitleController titleView;
        private ChapterSelectViewController chapterSelectView;
        private AlertController alert;
        private System.Random random;
        private bool inTransition = false;
        private int curStep;

        private void Awake()
        {
            var controller = Utils.FindNovaGameController();
            viewManager = Utils.FindViewManager();
            gameState = controller.GameState;
            dialogueBox = viewManager.GetController<DialogueBoxController>();
            saveView = viewManager.GetController<SaveViewController>();
            branchController = viewManager.GetComponentInChildren<BranchController>();
            helpView = viewManager.GetController<HelpViewController>();
            titleView = viewManager.GetController<TitleController>();
            chapterSelectView = viewManager.GetController<ChapterSelectViewController>();
            alert = viewManager.GetController<AlertController>();

            if (seed == 0)
            {
                seed = (int)DateTime.Now.Ticks & 0x0000FFFF;
            }
            random = new System.Random(seed);
        }

        private void Start()
        {
            if (steps > 0)
            {
                StartCoroutine(Mock());
            }
        }

        private WaitForSeconds delay => new WaitForSeconds(delaySeconds);

        private WaitWhile WaitForTransition()
        {
            return new WaitWhile(() => inTransition);
        }

        private WaitWhile DoTransition(Action<Action> action)
        {
            inTransition = true;
            action.Invoke(() => inTransition = false);
            return WaitForTransition();
        }

        private WaitWhile Show(ViewControllerBase view)
        {
            return DoTransition(view.Show);
        }

        private WaitWhile Hide(ViewControllerBase view)
        {
            return DoTransition(view.Hide);
        }

        private WaitWhile WaitForView(CurrentViewType viewType)
        {
            return new WaitWhile(() => viewManager.currentView != viewType);
        }

        private IEnumerator Mock()
        {
            curStep = 0;
            while (curStep < steps)
            {
                yield return delay;
                yield return StartCoroutine(MockTitle());
                yield return StartCoroutine(MockGame());
            }
            Alert.Show("", "Test Finished!");
        }

        private IEnumerator MockTitle()
        {
            yield return WaitForView(CurrentViewType.UI);
            if (helpView.myPanel.activeSelf)
            {
                yield return Hide(helpView);
            }
            yield return Show(chapterSelectView);
            var chapter = random.NextFromList(chapterSelectView.unlockedStartNodeNames);
            chapterSelectView.Hide(() => chapterSelectView.BeginChapter(chapter));
            yield return WaitForView(CurrentViewType.Game);
        }

        private IEnumerator MockSave()
        {
            yield return delay;
            if (random.NextInt(2) == 0)
            {
                saveView.QuickSaveBookmark();
            }
            else
            {
                // avoid page turn
                // we can maybe change this behaviour
                int index = (int)BookmarkType.NormalSave + random.NextInt(saveView.maxSaveEntry - 1);
                yield return DoTransition(saveView.ShowSaveWithCallback);
                yield return delay;
                saveView.SaveBookmark(index);
                yield return delay;
                yield return Hide(saveView);
                yield return WaitForView(CurrentViewType.Game);
            }
        }

        private IEnumerator MockGame()
        {
            while (true)
            {
                yield return delay;

                if (curStep >= steps)
                {
                    yield break;
                }

                if (gameState.isEnded)
                {
                    yield return WaitForView(CurrentViewType.UI);
                    yield break;
                }

                if (viewManager.currentView == CurrentViewType.Alert)
                {
                    yield return DoTransition(alert.Confirm);
                    yield return WaitForView(CurrentViewType.Game);
                }

                bool isAnimating = NovaAnimation.IsPlayingAny(AnimationType.PerDialogue);
                bool textIsAnimating = NovaAnimation.IsPlayingAny(AnimationType.Text);

                if (!gameState.canStepForward)
                {
                    var count = gameState.currentNode.branchCount;
                    branchController.Select(random.NextInt(count));
                }
                else if (!isAnimating && !textIsAnimating)
                {
                    if (random.NextDouble() < 0.1)
                    {
                        yield return StartCoroutine(MockSave());
                    }
                    else
                    {
                        dialogueBox.NextPageOrStep();
                    }
                }
                else if (fastForward)
                {
                    NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                }

                curStep++;
            }
        }
    }
}
