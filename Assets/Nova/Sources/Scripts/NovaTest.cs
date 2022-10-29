using System;
using System.Collections;
using UnityEngine;

namespace Nova
{
    public class NovaTest : MonoBehaviour
    {
        public int steps;
        public bool fastForward = true;
        public float delaySeconds = 0.001f;
        public int seed;

        private GameState gameState;
        private CheckpointManager checkpointManager;
        private ViewManager viewManager;
        private DialogueBoxController dialogueBox;
        private SaveViewController saveView;
        private BranchController branchController;
        private HelpViewController helpView;
        private TitleController titleView;
        private ChapterSelectViewController chapterSelectView;
        private AlertController alert;

        private System.Random random;
        private bool inTransition;
        private int curStep;

        private void Awake()
        {
            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;
            viewManager = Utils.FindViewManager();
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

        private WaitWhile DoTransition(Action<Action> action)
        {
            inTransition = true;
            action.Invoke(() => inTransition = false);
            return new WaitWhile(() => inTransition);
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

            Alert.Show(null, "test.finished");
        }

        private IEnumerator MockTitle()
        {
            yield return WaitForView(CurrentViewType.UI);
            if (helpView.myPanel.activeSelf)
            {
                yield return Hide(helpView);
            }

            yield return WaitForView(CurrentViewType.UI);

            var startNormalSave = (int)BookmarkType.NormalSave;
            var maxNormalSave = checkpointManager.QueryMinUnusedSaveID(startNormalSave);
            if (maxNormalSave > startNormalSave && random.NextInt(2) == 0)
            {
                var saveId = random.NextInt(startNormalSave, maxNormalSave);

                yield return DoTransition(onFinish => saveView.ShowLoadWithCallback(true, onFinish));
                yield return delay;
                saveView.LoadBookmark(saveId);
            }
            else
            {
                var chapters = gameState.GetAllUnlockedStartNodeNames();
                if (chapters.Count < 2)
                {
                    chapterSelectView.BeginChapter();
                }
                else
                {
                    yield return Show(chapterSelectView);
                    var chapter = random.NextFromList(chapters);
                    chapterSelectView.Hide(() => chapterSelectView.BeginChapter(chapter));
                }
            }

            yield return WaitForView(CurrentViewType.Game);
        }

        private IEnumerator MockSave()
        {
            if (random.NextInt(2) == 0)
            {
                saveView.QuickSaveBookmark();
            }
            else
            {
                var startSave = (int)BookmarkType.NormalSave;
                var maxNormalSave = checkpointManager.QueryMinUnusedSaveID(startSave);
                var saveId = random.NextInt(startSave, maxNormalSave + 1);

                yield return DoTransition(saveView.ShowSaveWithCallback);
                yield return delay;
                saveView.SaveBookmark(saveId);
                yield return delay;
                yield return Hide(saveView);
                yield return WaitForView(CurrentViewType.Game);
            }
        }

        private IEnumerator MockLoad()
        {
            var startQuickSave = (int)BookmarkType.QuickSave;
            var startNormalSave = (int)BookmarkType.NormalSave;
            var maxQuickSave = checkpointManager.QueryMinUnusedSaveID(startQuickSave);
            var maxNormalSave = checkpointManager.QueryMinUnusedSaveID(startNormalSave);
            if (maxQuickSave > startQuickSave && random.NextInt(2) == 0)
            {
                saveView.QuickLoadBookmark();
            }
            else if (maxNormalSave > startNormalSave)
            {
                var saveId = random.NextInt(startNormalSave, maxNormalSave);

                yield return DoTransition(onFinish => saveView.ShowLoadWithCallback(false, onFinish));
                yield return delay;
                saveView.LoadBookmark(saveId);
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
                    yield return delay;
                    yield return DoTransition(alert.Confirm);
                    yield return WaitForView(CurrentViewType.Game);
                }

                if (!gameState.canStepForward)
                {
                    yield return delay;
                    var count = gameState.currentNode.branchCount;
                    branchController.Select(random.NextInt(count));
                    curStep++;
                }
                else if (!NovaAnimation.IsPlayingAny(AnimationType.PerDialogue | AnimationType.Text))
                {
                    yield return delay;
                    if (random.NextDouble() < 0.1)
                    {
                        if (random.NextInt(2) == 0)
                        {
                            yield return StartCoroutine(MockSave());
                        }
                        else
                        {
                            yield return StartCoroutine(MockLoad());
                        }
                    }
                    else
                    {
                        dialogueBox.NextPageOrStep();
                    }

                    curStep++;
                }
                else if (fastForward)
                {
                    NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                }
            }
        }
    }
}
