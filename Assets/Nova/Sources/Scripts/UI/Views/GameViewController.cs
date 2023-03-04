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
        [SerializeField] private PanelController gameUIController;
        public DialogueBoxController currentDialogueBox;

        private GameState gameState;
        private DialogueState dialogueState;

        protected override bool Init()
        {
            if (base.Init())
            {
                return true;
            }

            var controller = Utils.FindNovaController();
            dialogueState = controller.DialogueState;
            gameState = controller.GameState;

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
            gameUIController.Show();
        }

        public void HideUI(Action onFinish = null)
        {
            gameUIController.Hide();
        }

        public void SwitchDialogueBox(DialogueBoxController box, bool cleanText = true)
        {
            if (currentDialogueBox == box)
            {
                box?.ShowImmediate();
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

        protected override void Update()
        {
            if (viewManager.currentView == CurrentViewType.Game && dialogueAvailable)
            {
                timeAfterDialogueChange += Time.deltaTime;

                if (currentDialogueBox != null && currentDialogueBox.dialogueFinishIconShown &&
                    dialogueState.isNormal &&
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

        #region Restoration

        public string restorableName => "GameViewController";

        [Serializable]
        private class GameViewControllerRestoreData : IRestoreData
        {
            public readonly string currentDialogueBox;

            public GameViewControllerRestoreData(GameViewController controller)
            {
                currentDialogueBox = controller.currentDialogueBox?.luaGlobalName ?? "";
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
                currentDialogueBox.ShowImmediate();
            }

            ShowUI();
        }

        #endregion
    }
}
