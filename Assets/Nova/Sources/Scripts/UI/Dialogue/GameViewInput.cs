using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Nova
{
    [ExportCustomType]
    public class GameViewInput : MonoBehaviour, IRestorable, IPointerDownHandler, IPointerUpHandler
    {
        private const string AbortAnimationFirstShownKey = ConfigManager.FirstShownKeyPrefix + "AbortAnimation";
        private const int HintAbortAnimationClicks = 10;

        private GameState gameState;
        private DialogueState dialogueState;
        private ConfigManager configManager;
        private InputManager inputManager;

        private ButtonRingTrigger buttonRingTrigger;

        private ViewManager viewManager;
        private DialogueBoxController dialogueBoxController;
        private SaveViewController saveViewController;
        private LogController logController;
        private ConfigViewController configViewController;

        private void Awake()
        {
            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            dialogueState = controller.DialogueState;
            configManager = controller.ConfigManager;
            inputManager = controller.InputManager;

            buttonRingTrigger = GetComponentInChildren<ButtonRingTrigger>();

            viewManager = Utils.FindViewManager();
            dialogueBoxController = viewManager.GetController<DialogueBoxController>();
            saveViewController = viewManager.GetController<SaveViewController>();
            logController = viewManager.GetController<LogController>();
            configViewController = viewManager.GetController<ConfigViewController>();

            LuaRuntime.Instance.BindObject("gameViewInput", this);
            gameState.AddRestorable(this);
        }

        private void OnDestroy()
        {
            gameState.RemoveRestorable(this);
        }

        private void Update()
        {
            HandleShortcut();
            HandleScroll();
        }

        private void HandleShortcutWhenDialogueShown()
        {
            if (inputManager.IsTriggered(AbstractKey.StepForward))
            {
                ClickForward();
            }

            if (inputManager.IsTriggered(AbstractKey.Auto))
            {
                dialogueState.state = DialogueState.State.Auto;
            }

            if (inputManager.IsTriggered(AbstractKey.Save))
            {
                saveViewController.ShowSave();
            }

            if (inputManager.IsTriggered(AbstractKey.Load))
            {
                saveViewController.ShowLoad();
            }

            if (inputManager.IsTriggered(AbstractKey.QuickSave))
            {
                saveViewController.QuickSaveBookmarkWithAlert();
            }

            if (inputManager.IsTriggered(AbstractKey.QuickLoad))
            {
                saveViewController.QuickLoadBookmarkWithAlert();
            }

            if (inputManager.IsTriggered(AbstractKey.ToggleDialogue))
            {
                dialogueBoxController.Hide();
            }

            if (inputManager.IsTriggered(AbstractKey.ShowLog))
            {
                logController.Show();
            }

            if (inputManager.IsTriggered(AbstractKey.ShowConfig))
            {
                configViewController.Show();
            }

            if (inputManager.IsTriggered(AbstractKey.ReturnTitle))
            {
                ReturnTitleWithAlert();
            }

            if (inputManager.IsTriggered(AbstractKey.QuitGame))
            {
                Utils.QuitWithAlert();
            }

            dialogueState.fastForwardHotKeyHolding = inputManager.IsPressed(AbstractKey.FastForward);
        }

        private void HandleShortcutWhenDialogueHidden()
        {
            if (inputManager.IsTriggered(AbstractKey.ToggleDialogue))
            {
                dialogueBoxController.Show();
            }
        }

        // All shortcuts should use inputMapper
        private void HandleShortcut()
        {
            if (buttonRingTrigger.buttonShowing)
            {
                if (inputManager.IsTriggered(AbstractKey.LeaveView))
                {
                    buttonRingTrigger.Hide(false);
                }
            }
            else
            {
                if (viewManager.currentView == CurrentViewType.Game)
                {
                    HandleShortcutWhenDialogueShown();
                }
                else if (viewManager.currentView == CurrentViewType.DialogueHidden)
                {
                    HandleShortcutWhenDialogueHidden();
                }
            }
        }

        [HideInInspector] public RightButtonAction rightButtonAction;

        private bool canTriggerButtonRing
        {
            get
            {
                var p = RealInput.pointerPosition;
                float r = buttonRingTrigger.sectorRadius * RealScreen.scale * 0.5f;
                return p.x > r && p.x < RealScreen.width - r && p.y > r && p.y < RealScreen.height - r;
            }
        }

        public void OnPointerDown(PointerEventData _eventData)
        {
            var eventData = (ExtendedPointerEventData)_eventData;
            if (!inputManager.inputEnabled ||
                viewManager.currentView != CurrentViewType.Game ||
                buttonRingTrigger.buttonShowing)
            {
                return;
            }

            // Stop auto/fast forward on any button or touch
            dialogueState.state = DialogueState.State.Normal;

            if (TouchFix.IsTouch(eventData) || eventData.button == PointerEventData.InputButton.Right)
            {
                if (canTriggerButtonRing)
                {
                    buttonRingTrigger.ShowIfPointerMoved();
                }
            }
        }

        public void OnPointerUp(PointerEventData _eventData)
        {
            var eventData = (ExtendedPointerEventData)_eventData;

            // When the input is not enabled, the user can only click forward
            if (!inputManager.inputEnabled)
            {
                if (TouchFix.IsTouch(eventData) || eventData.button == PointerEventData.InputButton.Left)
                {
                    ClickForward();
                }

                return;
            }

            var view = viewManager.currentView;
            if (view == CurrentViewType.DialogueHidden)
            {
                dialogueBoxController.Show();
                return;
            }

            if (view != CurrentViewType.Game)
            {
                return;
            }

            if (buttonRingTrigger.buttonShowing)
            {
                buttonRingTrigger.Hide(!buttonRingTrigger.holdOpen ||
                                       eventData.button == PointerEventData.InputButton.Left);
                return;
            }

            if (TouchFix.IsTouch(eventData) || eventData.button == PointerEventData.InputButton.Left)
            {
                buttonRingTrigger.NoShowIfPointerMoved();

                var link = dialogueBoxController.FindIntersectingLink(RealInput.pointerPosition, UICameraHelper.Active);
                if (!string.IsNullOrEmpty(link))
                {
                    Application.OpenURL(link);
                    return;
                }

                ClickForward();
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                buttonRingTrigger.NoShowIfPointerMoved();

                if (rightButtonAction == RightButtonAction.HideDialoguePanel)
                {
                    dialogueBoxController.Hide();
                }
                else if (rightButtonAction == RightButtonAction.ShowButtonRing)
                {
                    if (canTriggerButtonRing)
                    {
                        buttonRingTrigger.Show(true);
                    }
                }
            }
        }

        private void HandleScroll()
        {
            if (!inputManager.inputEnabled)
            {
                return;
            }

            // Ignore input when mouse is outside of the game window
            var mousePos = RealInput.mousePosition;
            if (mousePos.x < 0 || mousePos.x > RealScreen.width ||
                mousePos.y < 0 || mousePos.y > RealScreen.height)
            {
                return;
            }

            if (buttonRingTrigger.buttonShowing)
            {
                return;
            }

            if (viewManager.currentView == CurrentViewType.Game)
            {
                float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
                if (scroll > float.Epsilon)
                {
                    dialogueState.state = DialogueState.State.Normal;
                    logController.Show();
                }
                else if (scroll < -float.Epsilon)
                {
                    ClickForward();
                }
            }
        }

        [HideInInspector] public bool canClickForward = true;
        [HideInInspector] public bool canAbortAnimation = true;
        [HideInInspector] public bool scriptCanAbortAnimation = true;

        public void ClickForward()
        {
            if (!canClickForward)
            {
                return;
            }

            dialogueState.state = DialogueState.State.Normal;

            bool isAnimating = NovaAnimation.IsPlayingAny(AnimationType.PerDialogue);
            bool textIsAnimating = NovaAnimation.IsPlayingAny(AnimationType.Text);

            if (!isAnimating && !textIsAnimating)
            {
                dialogueBoxController.NextPageOrStep();
                return;
            }

            // When user clicks, text animation should stop, regardless of canAbortAnimation
            if (textIsAnimating)
            {
                NovaAnimation.StopAll(AnimationType.Text);
            }

            if (!scriptCanAbortAnimation)
            {
                return;
            }

            if (!canAbortAnimation)
            {
                int clicks = configManager.GetInt(AbortAnimationFirstShownKey);
                if (clicks < HintAbortAnimationClicks)
                {
                    configManager.SetInt(AbortAnimationFirstShownKey, clicks + 1);
                }
                else if (clicks == HintAbortAnimationClicks)
                {
                    Alert.Show("dialogue.hint.clickstopanimation");
                    configManager.SetInt(AbortAnimationFirstShownKey, clicks + 1);
                }

                return;
            }

            if (isAnimating)
            {
                NovaAnimation.StopAll(AnimationType.PerDialogue);
                dialogueBoxController.ShowDialogueFinishIcon(true);
            }
        }

        private void ReturnTitle()
        {
            NovaAnimation.StopAll();

            // TODO: Use a faster transition from game view to title view
            // viewManager.titlePanel.SetActive(true);

            dialogueBoxController.SwitchView<TitleController>();
        }

        private void ReturnTitleWithAlert()
        {
            Alert.Show(null, "ingame.title.confirm", ReturnTitle, null, "ReturnTitle");
        }

        #region Restoration

        public string restorableName => "GameViewInput";

        [Serializable]
        private class GameViewInputRestoreData : IRestoreData
        {
            public readonly bool canClickForward;
            public readonly bool scriptCanAbortAnimation;

            public GameViewInputRestoreData(bool canClickForward, bool scriptCanAbortAnimation)
            {
                this.canClickForward = canClickForward;
                this.scriptCanAbortAnimation = scriptCanAbortAnimation;
            }
        }

        public IRestoreData GetRestoreData()
        {
            return new GameViewInputRestoreData(canClickForward, scriptCanAbortAnimation);
        }

        public void Restore(IRestoreData restoreData)
        {
            var data = restoreData as GameViewInputRestoreData;
            canClickForward = data.canClickForward;
            scriptCanAbortAnimation = data.scriptCanAbortAnimation;
        }

        #endregion
    }
}
