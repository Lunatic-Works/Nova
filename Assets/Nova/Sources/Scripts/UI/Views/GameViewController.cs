using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class GameViewController : ViewControllerBase, IRestorable
    {
        [SerializeField] private GameObject autoModeIcon;
        [SerializeField] private GameObject fastForwardModeIcon;
        public DialogueBoxController currentDialogueBox;

        private GameState gameState;
        private DialogueState dialogueState;
        private GameUIController gameUIController;

        protected override bool Init()
        {
            if (base.Init())
            {
                return true;
            }

            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            dialogueState = controller.DialogueState;
            gameUIController = GetComponentInChildren<GameUIController>();

            LuaRuntime.Instance.BindObject("gameViewController", this);
            gameState.AddRestorable(this);

            return false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            gameState.RemoveRestorable(this);
        }

        private void OnEnable()
        {
            gameState.dialogueWillChange.AddListener(OnDialogueWillChange);
            gameState.dialogueChanged.AddListener(OnDialogueChanged);
            gameState.routeEnded.AddListener(OnRouteEnded);

            dialogueState.autoModeStarts.AddListener(OnAutoModeStarts);
            dialogueState.autoModeStops.AddListener(OnAutoModeStops);
            dialogueState.fastForwardModeStarts.AddListener(OnFastForwardModeStarts);
            dialogueState.fastForwardModeStops.AddListener(OnFastForwardModeStops);
        }

        private void OnDisable()
        {
            gameState.dialogueWillChange.RemoveListener(OnDialogueWillChange);
            gameState.dialogueChanged.RemoveListener(OnDialogueChanged);
            gameState.routeEnded.RemoveListener(OnRouteEnded);

            dialogueState.autoModeStarts.RemoveListener(OnAutoModeStarts);
            dialogueState.autoModeStops.RemoveListener(OnAutoModeStops);
            dialogueState.fastForwardModeStarts.RemoveListener(OnFastForwardModeStarts);
            dialogueState.fastForwardModeStops.RemoveListener(OnFastForwardModeStops);
        }

        public bool uiActive => gameUIController.active;

        public void ShowUI(Action onFinish = null)
        {
            gameUIController.Show(onFinish);
        }

        public void HideUI(Action onFinish = null)
        {
            gameUIController.Hide(onFinish);
        }

        public void SwitchDialogueBox(DialogueBoxController box, bool cleanText = true)
        {
            if (currentDialogueBox == box)
            {
                box?.ShowImmediate();
                // Do not clean text
                return;
            }

            currentDialogueBox?.HideImmediate();
            if (box != null)
            {
                box.ShowImmediate();
                if (cleanText)
                {
                    box.NewPage();
                }
            }

            currentDialogueBox = box;
        }

        public void Step()
        {
            if (currentDialogueBox == null || !currentDialogueBox.Forward())
            {
                gameState.Step();
            }
        }

        public void AbortAnimation(bool perDialogue)
        {
            if (currentDialogueBox?.active ?? false)
            {
                NovaAnimation.StopAll(AnimationType.Text);
                currentDialogueBox?.ShowDialogueFinishIcon(true);
            }

            if (perDialogue)
            {
                NovaAnimation.StopAll(AnimationType.PerDialogue);
                currentDialogueBox?.AbortTextAnimationDelay();
            }
        }

        public void ForceStep()
        {
            NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
            gameState.Step();
        }

        // TODO: Should enumerate all dialogue boxes
        public bool TryClickLink(Vector3 position, Camera camera)
        {
            if (currentDialogueBox == null)
            {
                return false;
            }

            var link = currentDialogueBox.FindIntersectingLink(position, camera);
            if (!string.IsNullOrEmpty(link))
            {
                Application.OpenURL(link);
                return true;
            }

            return false;
        }

        public float autoDelay { get; set; }
        public float fastForwardDelay { get; set; }
        private float timeAfterDialogueChange;
        private float dialogueTime = float.MaxValue;
        private bool dialogueAvailable;

        private void StopTimer()
        {
            timeAfterDialogueChange = 0f;
            dialogueAvailable = false;
        }

        private void RestartTimer()
        {
            timeAfterDialogueChange = 0f;
            dialogueAvailable = true;
        }

        private Coroutine scheduledStepCoroutine;

        private void TrySchedule(float scheduledDelay)
        {
            if (dialogueAvailable)
            {
                scheduledStepCoroutine = StartCoroutine(ScheduledStep(scheduledDelay));
            }
        }

        private void TryRemoveSchedule()
        {
            if (scheduledStepCoroutine == null) return;
            StopCoroutine(scheduledStepCoroutine);
            scheduledStepCoroutine = null;
        }

        private IEnumerator ScheduledStep(float scheduledDelay)
        {
            this.RuntimeAssert(dialogueAvailable, "Dialogue not available when scheduling a step for it.");

            while (scheduledDelay > timeAfterDialogueChange)
            {
                yield return new WaitForSeconds(scheduledDelay - timeAfterDialogueChange);
            }

            // Pause one frame before step
            // Give time for rendering and can stop schedule step in time before any unwanted effects occurs
            yield return null;

            if (gameState.canStepForward)
            {
                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                if (currentDialogueBox == null || !currentDialogueBox.Forward())
                {
                    gameState.Step();
                }
                else
                {
                    // TODO: Text animation when showing a new page
                    RestartTimer();
                    TrySchedule(dialogueState.isAuto ? GetDialogueTimeAutoText() : fastForwardDelay);
                }
            }
            else
            {
                dialogueState.state = DialogueState.State.Normal;
            }
        }

        // Check current state and set schedule for the next dialogue entry
        private void SetSchedule()
        {
            TryRemoveSchedule();
            switch (dialogueState.state)
            {
                case DialogueState.State.Normal:
                    break;
                case DialogueState.State.Auto:
                    TrySchedule(GetDialogueTimeAuto());
                    break;
                case DialogueState.State.FastForward:
                    TrySchedule(fastForwardDelay);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static float GetDialogueTime(float offset = 0.0f, float voiceOffset = 0.0f)
        {
            return Mathf.Max(
                NovaAnimation.GetTotalTimeRemaining(AnimationType.PerDialogue | AnimationType.Text) + offset,
                GameCharacterController.MaxVoiceDuration + voiceOffset
            );
        }

        private float GetDialogueTimeAutoText()
        {
            return currentDialogueBox == null
                ? 0f
                : currentDialogueBox.GetPageCharacterCount() * autoDelay * 0.1f + autoDelay;
        }

        private float GetDialogueTimeAuto()
        {
            return Mathf.Max(GetDialogueTime(autoDelay, autoDelay * 0.5f), GetDialogueTimeAutoText());
        }

        protected override void Update()
        {
            if (viewManager.currentView == CurrentViewType.Game && dialogueAvailable)
            {
                timeAfterDialogueChange += Time.deltaTime;

                if (currentDialogueBox != null && currentDialogueBox.dialogueFinishIconShown &&
                    dialogueState.isNormal && timeAfterDialogueChange > dialogueTime)
                {
                    currentDialogueBox.ShowDialogueFinishIcon(true);
                }
            }
        }

        private void OnDialogueWillChange()
        {
            StopTimer();
            currentDialogueBox?.OnDialogueWillChange();
        }

        private void OnDialogueChanged(DialogueChangedData dialogueData)
        {
            RestartTimer();
            currentDialogueBox?.DisplayDialogue(dialogueData.displayData);
            SetSchedule();
            dialogueTime = GetDialogueTime();
        }

        private void OnRouteEnded(RouteEndedData routeEndedData)
        {
            dialogueState.state = DialogueState.State.Normal;
            this.SwitchView<TitleController>();
        }

        private void OnAutoModeStarts()
        {
            TrySchedule(Mathf.Max(GetDialogueTimeAuto() - timeAfterDialogueChange, 0f));

            if (autoModeIcon != null)
            {
                autoModeIcon.SetActive(true);
            }
        }

        private void OnAutoModeStops()
        {
            TryRemoveSchedule();

            if (autoModeIcon != null)
            {
                autoModeIcon.SetActive(false);
            }
        }

        private void OnFastForwardModeStarts()
        {
            TrySchedule(fastForwardDelay);

            if (fastForwardModeIcon != null)
            {
                fastForwardModeIcon.SetActive(true);
            }
        }

        private void OnFastForwardModeStops()
        {
            TryRemoveSchedule();

            if (fastForwardModeIcon != null)
            {
                fastForwardModeIcon.SetActive(false);
            }
        }

        #region Restoration

        public string restorableName => "GameViewController";

        [Serializable]
        private class GameViewControllerRestoreData : IRestoreData
        {
            public readonly string currentDialogueBox;

            public GameViewControllerRestoreData(GameViewController parent)
            {
                currentDialogueBox = parent.currentDialogueBox?.luaGlobalName ?? "";
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new GameViewControllerRestoreData(this);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as GameViewControllerRestoreData;
            if (!string.IsNullOrEmpty(data.currentDialogueBox))
            {
                currentDialogueBox = GetComponentsInChildren<DialogueBoxController>(true)
                    .First(x => x.luaGlobalName == data.currentDialogueBox);
            }
            else
            {
                currentDialogueBox = null;
            }

            // All DialogueBoxController will restore their show/hide state respectively

            ShowUI();
        }

        #endregion
    }
}
