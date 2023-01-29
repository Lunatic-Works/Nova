using System;
using System.Collections;
using UnityEngine;

namespace Nova
{
    public class GameViewController : MonoBehaviour, IViewController
    {
        [SerializeField] private GameObject autoModeIcon;
        [SerializeField] private GameObject fastForwardModeIcon;
        public DialogueBoxController currentDialogueBox;

        public ViewManager viewManager { get; private set; }
        private GameState gameState;
        private DialogueState dialogueState;

        private void Awake()
        {
            viewManager = GetComponentInParent<ViewManager>();
            viewManager.SetController(this);
            var controller = Utils.FindNovaController();
            dialogueState = controller.DialogueState;
            gameState = controller.GameState;
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

        public bool active => true;

        public bool dialogueBoxActive => currentDialogueBox?.active ?? false;

        public void Show(Action onFinish)
        {
            currentDialogueBox?.Show(onFinish);
        }

        public void Hide(Action onFinish)
        {
            currentDialogueBox?.Hide(onFinish);
        }

        public void ShowImmediate(Action onFinish)
        {
            currentDialogueBox?.ShowImmediate(onFinish);
        }

        public void HideImmediate(Action onFinish)
        {
            currentDialogueBox?.HideImmediate(onFinish);
        }

        public void Step()
        {
            if (currentDialogueBox != null)
            {
                currentDialogueBox.NextPageOrStep();
            }
            else
            {
                gameState.Step();
            }
        }

        public void AbortAnimation(bool perDialogue)
        {
            var animation = AnimationType.Text;
            if (perDialogue)
            {
                animation |= AnimationType.PerDialogue;
            }
            NovaAnimation.StopAll(animation);
            currentDialogueBox?.ShowDialogueFinishIcon(true);
        }

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

            if (gameState.canStepForward && currentDialogueBox != null)
            {
                NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                if (currentDialogueBox.NextPageOrStep())
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
            if (currentDialogueBox != null)
            {
                delay = Mathf.Max(delay, NovaAnimation.GetTotalTimeRemaining(AnimationType.Text) /
                    currentDialogueBox.characterFadeInDuration * autoDelay * 0.1f);
            }
            return delay;
        }

        private void Update()
        {
            if (viewManager.currentView == CurrentViewType.Game && dialogueAvailable)
            {
                timeAfterDialogueChange += Time.deltaTime;

                Debug.Log($"{timeAfterDialogueChange}, {dialogueTime}");

                if (currentDialogueBox != null && currentDialogueBox.dialogueFinishIconShown && dialogueState.isNormal &&
                    viewManager.currentView != CurrentViewType.InTransition && timeAfterDialogueChange > dialogueTime)
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
