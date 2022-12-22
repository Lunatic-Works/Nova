using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class MockInput : MonoBehaviour
    {
        [SerializeField] private int steps;
        [SerializeField] private bool unlockAllChapters;
        [SerializeField] private bool unlockDebugChapters;
        [SerializeField] private bool fastForward = true;
        [SerializeField] private float delaySeconds = 0.001f;
        [SerializeField] private bool canGoBack = true;
        [SerializeField] private float saveRate = 0.01f;
        [SerializeField] private float loadRate = 0.01f;
        [SerializeField] private float logMoveBackRate = 0.01f;
        [SerializeField] private float returnTitleRate = 0.01f;
        [SerializeField] private int seed;

        private GameState gameState;
        private CheckpointManager checkpointManager;
        private ViewManager viewManager;
        private DialogueBoxController dialogueBox;
        private SaveViewController saveView;
        private LogController logView;
        private ConfigViewController configView;
        private BranchController branchController;
        private HelpViewController helpView;
        private ChapterSelectViewController chapterSelectView;
        private AlertController alert;

        private System.Random random;

        private void Awake()
        {
            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;
            viewManager = Utils.FindViewManager();
            dialogueBox = viewManager.GetController<DialogueBoxController>();
            saveView = viewManager.GetController<SaveViewController>();
            logView = viewManager.GetController<LogController>();
            configView = viewManager.GetController<ConfigViewController>();
            branchController = viewManager.GetComponentInChildren<BranchController>();
            helpView = viewManager.GetController<HelpViewController>();
            chapterSelectView = viewManager.GetController<ChapterSelectViewController>();
            alert = viewManager.GetController<AlertController>();
        }

        private void OnEnable()
        {
            if (steps <= 0)
            {
                return;
            }

            if (seed == 0)
            {
                seed = (int)DateTime.Now.Ticks & 0xffff;
            }

            random = new System.Random(seed);
            StartCoroutine(Mock());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            steps = 0;
        }

        private WaitForSeconds delay => new WaitForSeconds(delaySeconds);

        private static WaitWhile DoTransition(Action<Action> action)
        {
            var inTransition = true;
            action.Invoke(() => inTransition = false);
            return new WaitWhile(() => inTransition);
        }

        private static WaitWhile Show(ViewControllerBase view)
        {
            return DoTransition(view.Show);
        }

        private static WaitWhile Hide(ViewControllerBase view)
        {
            return DoTransition(view.Hide);
        }

        private WaitUntil WaitForView(CurrentViewType viewType)
        {
            return new WaitUntil(() => viewManager.currentView == viewType);
        }

        private IEnumerator Mock()
        {
            while (steps > 0)
            {
                yield return StartCoroutine(MockTitle());
                yield return StartCoroutine(MockGame());
            }

            Alert.Show(null, "test.finished");
        }

        private IEnumerator MockTitle()
        {
            yield return delay;

            if (!gameState.isEnded)
            {
                if (viewManager.currentView != CurrentViewType.Game)
                {
                    Debug.Log("Waiting for game view");
                }

                yield return WaitForView(CurrentViewType.Game);
                yield break;
            }

            yield return WaitForView(CurrentViewType.UI);
            if (helpView.myPanel.activeSelf)
            {
                yield return Hide(helpView);
            }

            yield return WaitForView(CurrentViewType.UI);

            var startNormalSave = (int)BookmarkType.NormalSave;
            var maxNormalSave = checkpointManager.QueryMinUnusedSaveID(startNormalSave);
            if (maxNormalSave > startNormalSave && random.Next(2) == 0)
            {
                var saveId = random.Next(startNormalSave, maxNormalSave);

                yield return DoTransition(onFinish => saveView.ShowLoadWithCallback(true, onFinish));
                yield return delay;
                saveView.LoadBookmark(saveId);
            }
            else
            {
                if (unlockAllChapters || unlockDebugChapters)
                {
                    chapterSelectView.UnlockChapters(unlockAllChapters, unlockDebugChapters);
                }

                yield return Show(chapterSelectView);
                yield return delay;
                var chapter = random.Next(chapterSelectView.GetUnlockedChapters().ToList());
                chapterSelectView.Hide(() => chapterSelectView.BeginChapter(chapter));
            }

            yield return WaitForView(CurrentViewType.Game);
        }

        private IEnumerator MockSave()
        {
            if (random.Next(2) == 0)
            {
                saveView.QuickSaveBookmark();
            }
            else
            {
                var startSave = (int)BookmarkType.NormalSave;
                var maxNormalSave = checkpointManager.QueryMinUnusedSaveID(startSave);
                var saveId = random.Next(startSave, maxNormalSave + 1);

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
            if (maxQuickSave > startQuickSave && random.Next(2) == 0)
            {
                saveView.QuickLoadBookmark();
            }
            else if (maxNormalSave > startNormalSave)
            {
                var saveId = random.Next(startNormalSave, maxNormalSave);

                yield return DoTransition(onFinish => saveView.ShowLoadWithCallback(false, onFinish));
                yield return delay;
                saveView.LoadBookmark(saveId);
                yield return WaitForView(CurrentViewType.Game);
            }
        }

        private IEnumerator MockLogMoveBack()
        {
            var logEntry = logView.GetRandomLogEntry(random);

            yield return Show(logView);
            yield return delay;
            yield return DoTransition(onFinish => logView.MoveBackWithCallback(logEntry, onFinish));
        }

        private IEnumerator MockReturnTitle()
        {
            yield return Show(configView);
            yield return delay;
            yield return DoTransition(configView.ReturnTitleWithCallback);
        }

        private IEnumerator MockGame()
        {
            while (true)
            {
                yield return delay;

                if (steps <= 0)
                {
                    yield break;
                }

                if (gameState.isEnded)
                {
                    yield return WaitForView(CurrentViewType.UI);
                    yield break;
                }

                if (viewManager.currentView == CurrentViewType.InTransition)
                {
                    yield return new WaitUntil(() => viewManager.currentView != CurrentViewType.InTransition);
                }

                if (viewManager.currentView == CurrentViewType.Alert)
                {
                    yield return DoTransition(alert.Confirm);
                    yield return delay;
                }

                if (viewManager.currentView != CurrentViewType.Game)
                {
                    Debug.Log("Waiting for game view");
                }

                yield return WaitForView(CurrentViewType.Game);

                if (!gameState.canStepForward)
                {
                    // TODO: Handle minigames
                    yield return new WaitUntil(() => branchController.activeSelectionCount > 0);
                    // TODO: Only select from interactable selections
                    branchController.Select(random.Next(branchController.activeSelectionCount));
                    steps--;
                }
                else if (!NovaAnimation.IsPlayingAny(AnimationType.PerDialogue | AnimationType.Text))
                {
                    var r = random.NextDouble();
                    if (r < saveRate)
                    {
                        yield return StartCoroutine(MockSave());
                    }
                    else if (canGoBack && r < saveRate + loadRate)
                    {
                        yield return StartCoroutine(MockLoad());
                    }
                    else if (canGoBack && r < saveRate + loadRate + logMoveBackRate)
                    {
                        yield return StartCoroutine(MockLogMoveBack());
                    }
                    else if (canGoBack && r < saveRate + loadRate + logMoveBackRate + returnTitleRate)
                    {
                        yield return StartCoroutine(MockReturnTitle());
                        yield break;
                    }
                    else
                    {
                        dialogueBox.NextPageOrStep();
                    }

                    steps--;
                }
                else if (fastForward)
                {
                    NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                }
            }
        }
    }
}
