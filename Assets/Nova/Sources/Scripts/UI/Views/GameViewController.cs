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
            gameState.choiceOccurs.AddListener(OnChoiceOccurs);
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
            gameState.choiceOccurs.RemoveListener(OnChoiceOccurs);
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
                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                gameState.Step();
            }
        }

        public void AbortAnimation(bool perDialogue)
        {
            var dirty = false;
            if (currentDialogueBox != null && currentDialogueBox.active)
            {
                NovaAnimation.StopAll(AnimationType.Text);
                dirty = true;
            }

            if (perDialogue)
            {
                NovaAnimation.StopAll(AnimationType.PerDialogue);
                currentDialogueBox?.AbortTextAnimationDelay();
                dirty = true;
            }

            if (dirty)
            {
                dialogueTime = timeAfterDialogueChange + GetDialogueTimeTextVoice();
            }
        }

        // TODO: Should enumerate all dialogue boxes
        public bool TryClickLink(Vector3 position, Camera camera)
        {
            if (currentDialogueBox == null || !currentDialogueBox.active)
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

        private float autoTimeOverride = -1f;
        private bool immediateStep = false;

        public void OverrideAutoTime(float secs)
        {
            autoTimeOverride = Mathf.Max(secs, 0f);
        }

        public void ScheduleImmediateStep()
        {
            immediateStep = true;
        }

        public float autoDelay { get; set; }
        public float fastForwardDelay { get; set; }
        private float timeAfterDialogueChange;
        private float dialogueTime = float.MaxValue;
        private bool timerStarted;

        private void StopTimer()
        {
            timeAfterDialogueChange = 0f;
            timerStarted = false;
        }

        private void RestartTimer()
        {
            timeAfterDialogueChange = 0f;
            timerStarted = true;
        }

        private Coroutine scheduledStepCoroutine;

        private void TrySchedule(float scheduledDelay)
        {
            if (timerStarted)
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
            this.RuntimeAssert(timerStarted, "Timer not started when scheduling a step for it.");

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
                    TrySchedule(dialogueState.isAuto ? GetDialogueTimeAuto() : fastForwardDelay);
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

        private float GetDialogueTimeTextVoice(float offset = 0.0f, float voiceOffset = 0.0f)
        {
            return Mathf.Max(
                NovaAnimation.GetTotalTimeRemaining(AnimationType.PerDialogue | AnimationType.Text) + offset,
                Mathf.Max(GameCharacterController.MaxVoiceDuration - timeAfterDialogueChange, 0f) + voiceOffset
            );
        }

        private float GetDialogueTimeAutoText()
        {
            float textAnimationDelay = currentDialogueBox?.textAnimationDelay ?? 0f;
            int characterCount = currentDialogueBox?.GetPageCharacterCount() ?? 0;
            float factor = 0.1f * characterCount + 0.5f + 0.5f / (1 + characterCount);
            return Mathf.Max(textAnimationDelay + autoDelay * factor - timeAfterDialogueChange, 0f);
        }

        private float GetDialogueTimeAuto()
        {
            if (autoTimeOverride >= 0f)
            {
                return autoTimeOverride;
            }

            return Mathf.Max(GetDialogueTimeTextVoice(autoDelay, autoDelay), GetDialogueTimeAutoText());
        }

        protected override void Update()
        {
            if (viewManager.currentView != CurrentViewType.Game || !timerStarted)
            {
                return;
            }

            timeAfterDialogueChange += Time.deltaTime;
            if (dialogueState.isNormal && timeAfterDialogueChange > dialogueTime)
            {
                if (immediateStep)
                {
                    // All AnimationType.PerDialogue | AnimationType.Text should already be stopped,
                    // but to prevent timing problems here we stop them again
                    NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                    gameState.Step();
                }
                else
                {
                    if (currentDialogueBox != null && currentDialogueBox.dialogueFinishIconShown)
                    {
                        currentDialogueBox.ShowDialogueFinishIcon(true);
                    }
                }
            }
        }

        private void OnDialogueWillChange()
        {
            StopTimer();
            autoTimeOverride = -1f;
            immediateStep = false;
        }

        private void OnDialogueChanged(DialogueChangedData dialogueData)
        {
            RestartTimer();
            currentDialogueBox?.DisplayDialogue(dialogueData.displayData);
            SetSchedule();
            dialogueTime = GetDialogueTimeTextVoice();
        }

        private void OnChoiceOccurs(ChoiceOccursData _)
        {
            StopTimer();
        }

        private void OnRouteEnded(RouteEndedData routeEndedData)
        {
            dialogueState.state = DialogueState.State.Normal;
            this.SwitchView<TitleController>();
        }

        private void OnAutoModeStarts()
        {
            TrySchedule(GetDialogueTimeAuto());

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
