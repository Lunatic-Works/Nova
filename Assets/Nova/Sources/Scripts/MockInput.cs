using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class MockInput : MonoBehaviour
    {
        [SerializeField] private int steps;
        [SerializeField] private bool unlockAllNodes;
        [SerializeField] private bool unlockDebugNodes;
        [SerializeField] private bool fastForward = true;
        [SerializeField] private bool canGoBack = true;
        [SerializeField] private float saveRate = 0.01f;
        [SerializeField] private float loadRate = 0.01f;
        [SerializeField] private float logMoveBackRate = 0.01f;
        [SerializeField] private float returnTitleRate = 0.01f;
        [SerializeField] private int seed;

        private GameState gameState;
        private CheckpointManager checkpointManager;
        private ViewManager viewManager;
        private GameViewController gameView;
        private SaveViewController saveView;
        private LogController logView;
        private ConfigViewController configView;
        private ChoicesController choicesController;
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
            gameView = viewManager.GetController<GameViewController>();
            saveView = viewManager.GetController<SaveViewController>();
            logView = viewManager.GetController<LogController>();
            configView = viewManager.GetController<ConfigViewController>();
            choicesController = viewManager.GetComponentInChildren<ChoicesController>(true);
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

        private WaitWhile DoTransition(Action<Action> action)
        {
            var inTransition = true;
            action.Invoke(() => inTransition = false);
            // action may be DelayedShow, so we try to stop animation in every frame
            return new WaitWhile(() => {
                if (fastForward)
                {
                    NovaAnimation.StopAll(AnimationType.UI);
                }

                return inTransition;
            });
        }

        private WaitWhile Show(IViewController view)
        {
            return DoTransition(view.Show);
        }

        private WaitWhile Hide(IViewController view)
        {
            return DoTransition(view.Hide);
        }

        private WaitUntil WaitForView(CurrentViewType viewType)
        {
            return new WaitUntil(() => {
                if (fastForward)
                {
                    NovaAnimation.StopAll(AnimationType.UI);
                }

                return viewManager.currentView == viewType;
            });
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
            if (helpView.active)
            {
                yield return Hide(helpView);
            }

            yield return WaitForView(CurrentViewType.UI);

            var startNormalSave = (int)BookmarkType.NormalSave;
            var maxNormalSave = checkpointManager.QueryMinUnusedSaveID(startNormalSave);
            if (maxNormalSave > startNormalSave && random.Next(2) == 0)
            {
                var saveID = random.Next(startNormalSave, maxNormalSave);

                yield return DoTransition(onFinish => saveView.ShowLoadWithCallback(true, onFinish));
                saveView.LoadBookmark(saveID);
            }
            else
            {
                if (unlockAllNodes || unlockDebugNodes)
                {
                    chapterSelectView.UnlockNodes(unlockAllNodes, unlockDebugNodes);
                }

                yield return Show(chapterSelectView);
                var node = random.Next(chapterSelectView.GetUnlockedNodes().ToList());
                yield return DoTransition(onFinish => chapterSelectView.Hide(() => {
                    chapterSelectView.GameStart(node);
                    onFinish?.Invoke();
                }));
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
                var saveID = random.Next(startSave, maxNormalSave + 1);

                yield return DoTransition(saveView.ShowSaveWithCallback);
                saveView.SaveBookmark(saveID);
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
                var saveID = random.Next(startNormalSave, maxNormalSave);

                yield return DoTransition(onFinish => saveView.ShowLoadWithCallback(false, onFinish));
                saveView.LoadBookmark(saveID);
                yield return WaitForView(CurrentViewType.Game);
            }
        }

        private IEnumerator MockLogMoveBack()
        {
            var logEntry = logView.GetRandomLogEntry(random);

            yield return Show(logView);
            yield return DoTransition(onFinish => logView.MoveBackWithCallback(logEntry, onFinish));
        }

        private IEnumerator MockReturnTitle()
        {
            yield return Show(configView);
            yield return DoTransition(configView.ReturnTitleWithCallback);
        }

        private IEnumerator MockGame()
        {
            while (true)
            {
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
                }

                if (viewManager.currentView != CurrentViewType.Game)
                {
                    Debug.Log("Waiting for game view");
                }

                yield return WaitForView(CurrentViewType.Game);

                if (!gameState.canStepForward)
                {
                    // TODO: Handle minigames
                    yield return new WaitUntil(() => choicesController.activeChoiceCount > 0);
                    // TODO: Only select from interactable choices
                    choicesController.Select(random.Next(choicesController.activeChoiceCount));
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
                        gameView.Step();
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
