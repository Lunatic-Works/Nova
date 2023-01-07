using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    [ExportCustomType]
    public class GameViewInput : MonoBehaviour, IRestorable, IPointerDownHandler, IPointerUpHandler
    {
        private const string AbortAnimationFirstShownKey = ConfigManager.FirstShownKeyPrefix + "AbortAnimation";
        private const int HintAbortAnimationClicks = 10;

        private NovaController novaController;
        private GameState gameState;
        private DialogueState dialogueState;
        private ConfigManager configManager;
        private InputMapper inputMapper;

        private ButtonRingTrigger buttonRingTrigger;

        private ViewManager viewManager;
        private DialogueBoxController dialogueBoxController;
        private SaveViewController saveViewController;
        private LogController logController;
        private ConfigViewController configViewController;

        private void Awake()
        {
            novaController = Utils.FindNovaController();
            gameState = novaController.GameState;
            dialogueState = novaController.DialogueState;
            configManager = novaController.ConfigManager;
            inputMapper = novaController.InputMapper;

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
            if (inputMapper.GetKeyUp(AbstractKey.StepForward))
            {
                ClickForward();
            }

            if (inputMapper.GetKeyUp(AbstractKey.Auto))
            {
                dialogueState.state = DialogueState.State.Auto;
            }

            if (inputMapper.GetKeyUp(AbstractKey.Save))
            {
                saveViewController.ShowSave();
            }

            if (inputMapper.GetKeyUp(AbstractKey.Load))
            {
                saveViewController.ShowLoad();
            }

            if (inputMapper.GetKeyUp(AbstractKey.QuickSave))
            {
                saveViewController.QuickSaveBookmarkWithAlert();
            }

            if (inputMapper.GetKeyUp(AbstractKey.QuickLoad))
            {
                saveViewController.QuickLoadBookmarkWithAlert();
            }

            if (inputMapper.GetKeyUp(AbstractKey.ToggleDialogue))
            {
                dialogueBoxController.Hide();
            }

            if (inputMapper.GetKeyUp(AbstractKey.ShowLog))
            {
                logController.Show();
            }

            if (inputMapper.GetKeyUp(AbstractKey.ShowConfig))
            {
                configViewController.Show();
            }

            dialogueState.fastForwardHotKeyHolding = inputMapper.GetKey(AbstractKey.FastForward);
        }

        private void HandleShortcutWhenDialogueHidden()
        {
            if (inputMapper.GetKeyUp(AbstractKey.ToggleDialogue))
            {
                dialogueBoxController.Show();
            }
        }

        // All shortcuts should use inputMapper
        private void HandleShortcut()
        {
            if (buttonRingTrigger.buttonShowing)
            {
                if (inputMapper.GetKeyUp(AbstractKey.LeaveView))
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
                var p = RealInput.mousePosition;
                float r = buttonRingTrigger.sectorRadius * RealScreen.scale * 0.5f;
                return p.x > r && p.x < RealScreen.width - r && p.y > r && p.y < RealScreen.height - r;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!novaController.inputEnabled ||
                viewManager.currentView != CurrentViewType.Game ||
                buttonRingTrigger.buttonShowing)
            {
                return;
            }

            // Stop auto/fast forward on any button or touch
            dialogueState.state = DialogueState.State.Normal;

            if (Utils.IsTouch(eventData) || Utils.IsRightButton(eventData))
            {
                if (canTriggerButtonRing)
                {
                    buttonRingTrigger.ShowIfPointerMoved();
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // When the input is not enabled, the user can only click forward
            if (!novaController.inputEnabled)
            {
                if (Utils.IsTouch(eventData) || Utils.IsLeftButton(eventData))
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
                buttonRingTrigger.Hide(!buttonRingTrigger.holdOpen || Utils.IsLeftButton(eventData));
                return;
            }

            if (Utils.IsTouch(eventData) || Utils.IsLeftButton(eventData))
            {
                buttonRingTrigger.NoShowIfPointerMoved();

                var link = dialogueBoxController.FindIntersectingLink(RealInput.mousePosition, UICameraHelper.Active);
                if (!string.IsNullOrEmpty(link))
                {
                    Application.OpenURL(link);
                    return;
                }

                ClickForward();
                return;
            }

            if (Utils.IsRightButton(eventData))
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
            if (!novaController.inputEnabled)
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
                float scroll = Input.mouseScrollDelta.y;
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
