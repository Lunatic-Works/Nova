using System;
using System.Collections;
using UnityEngine;

namespace Nova
{
    public class GameViewController : ViewControllerBase
    {
        [SerializeField] private GameObject autoModeIcon;
        [SerializeField] private GameObject fastForwardModeIcon;

        private GameState gameState;
        private DialogueState dialogueState;
        public DialogueBoxController activeDialogueBox;

        protected override bool Init()
        {
            if (base.Init())
            {
                return true;
            }

            var controller = Utils.FindNovaController();
            dialogueState = controller.DialogueState;
            gameState = controller.GameState;

            return false;
        }

        private void onEnable()
        {
            gameState.dialogueWillChange.AddListener(OnDialogueWillChange);
            gameState.dialogueChanged.AddListener(OnDialogueChanged);
            dialogueState.autoModeStarts.AddListener(OnAutoModeStarts);
            dialogueState.autoModeStops.AddListener(OnAutoModeStops);
            dialogueState.fastForwardModeStarts.AddListener(OnFastForwardModeStarts);
            dialogueState.fastForwardModeStops.AddListener(OnFastForwardModeStops);
        }

        private void onDisable()
        {
            gameState.dialogueWillChange.RemoveListener(OnDialogueWillChange);
            gameState.dialogueChanged.RemoveListener(OnDialogueChanged);
            dialogueState.autoModeStarts.RemoveListener(OnAutoModeStarts);
            dialogueState.autoModeStops.RemoveListener(OnAutoModeStops);
            dialogueState.fastForwardModeStarts.RemoveListener(OnFastForwardModeStarts);
            dialogueState.fastForwardModeStops.RemoveListener(OnFastForwardModeStops);
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

            if (gameState.canStepForward && activeDialogueBox != null)
            {
                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                if (activeDialogueBox.NextPageOrStep())
                {
                    timeAfterDialogueChange = 0f;
                    TrySchedule(dialogueState.isAuto ? autoDelay : fastForwardDelay);
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

        private float GetDialogueTimeAuto()
        {
            var delay = GetDialogueTime(autoDelay, autoDelay * 0.5f);
            if (activeDialogueBox != null)
            {
                delay = Mathf.Max(delay, NovaAnimation.GetTotalTimeRemaining(AnimationType.Text) /
                    activeDialogueBox.characterFadeInDuration * autoDelay * 0.1f);
            }
            return delay;
        }

        protected override void Update()
        {
            if (viewManager.currentView == CurrentViewType.Game && dialogueAvailable)
            {
                timeAfterDialogueChange += Time.deltaTime;

                if (activeDialogueBox != null && activeDialogueBox.showDialogueFinishIcon && dialogueState.isNormal &&
                    viewManager.currentView != CurrentViewType.InTransition && timeAfterDialogueChange > dialogueTime)
                {
                    activeDialogueBox.ShowDialogueFinishIcon(true);
                }
            }
        }

        private void OnDialogueWillChange()
        {
            StopTimer();
        }

        private void OnDialogueChanged(DialogueChangedData dialogueData)
        {
            RestartTimer();
            SetSchedule();
            dialogueTime = GetDialogueTime();
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

    }
}
