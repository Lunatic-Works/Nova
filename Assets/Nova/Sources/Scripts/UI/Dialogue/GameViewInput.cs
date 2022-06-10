﻿using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    [ExportCustomType]
    public class GameViewInput : MonoBehaviour, IRestorable, IPointerDownHandler, IPointerUpHandler
    {
        private const string AbortAnimationFirstShownKey = ConfigManager.FirstShownKeyPrefix + "AbortAnimation";
        private const int HintAbortAnimationClicks = 10;

        private GameController gameController;
        private GameState gameState;
        private DialogueState dialogueState;
        private ConfigManager configManager;
        private InputMapper inputMapper;

        private ButtonRingTrigger buttonRingTrigger;

        private ViewManager viewManager;
        private DialogueBoxController dialogueBoxController;
        private SaveViewController saveViewController;
        private LogController logController;

        private void Awake()
        {
            gameController = Utils.FindNovaGameController();
            gameState = gameController.GameState;
            dialogueState = gameController.DialogueState;
            configManager = gameController.ConfigManager;
            inputMapper = gameController.InputMapper;

            buttonRingTrigger = GetComponentInChildren<ButtonRingTrigger>();

            viewManager = Utils.FindViewManager();
            dialogueBoxController = viewManager.GetController<DialogueBoxController>();
            saveViewController = viewManager.GetController<SaveViewController>();
            logController = viewManager.GetController<LogController>();

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

            if (gameController.inputEnabled)
            {
                HandleInput();
            }
        }

        private void HandleShortcutWhenDialogueShown()
        {
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
                saveViewController.QuickSaveBookmark();
            }

            if (inputMapper.GetKeyUp(AbstractKey.QuickLoad))
            {
                saveViewController.QuickLoadBookmark();
            }

            if (inputMapper.GetKeyUp(AbstractKey.ToggleDialogue))
            {
                dialogueBoxController.Hide();
            }

            if (inputMapper.GetKeyUp(AbstractKey.StepForward))
            {
                ClickForward();
            }

            if (inputMapper.GetKeyUp(AbstractKey.ShowLog))
            {
                logController.Show();
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

        private bool skipNextTouch;
        private bool skipTouchOnPointerUp;

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!gameController.inputEnabled)
            {
                // Touch finger
                if (eventData.pointerId >= 0)
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
                buttonRingTrigger.Hide(!buttonRingTrigger.holdOpen || eventData.pointerId != -2);
                return;
            }

            // Mouse right button
            // We do not use two-finger tap to simulate right button
            // if (eventData.pointerId == -2 || Input.touchCount == 2)
            if (eventData.pointerId == -2)
            {
                if (!buttonRingTrigger.buttonShowing)
                {
                    buttonRingTrigger.NoShowIfMouseMoved();
                    if (rightButtonAction == RightButtonAction.HideDialoguePanel)
                    {
                        dialogueBoxController.Hide();
                    }
                    else if (rightButtonAction == RightButtonAction.ShowButtonRing)
                    {
                        float r = buttonRingTrigger.sectorRadius * RealScreen.fWidth / 1920 * 0.5f;
                        if (RealInput.mousePosition.x > r && RealInput.mousePosition.x < RealScreen.width - r &&
                            RealInput.mousePosition.y > r && RealInput.mousePosition.y < RealScreen.height - r)
                        {
                            buttonRingTrigger.Show(true);
                        }
                    }
                }
                else if (!buttonRingTrigger.holdOpen)
                {
                    buttonRingTrigger.Hide(true);
                }

                // if (Input.touchCount == 2)
                // {
                //     skipNextTouch = true;
                // }
            }
            else
            {
                // Touch finger
                // (consequent touch will be converted to 1 / 2 / ... due to unknown reason)
                if (eventData.pointerId >= 0)
                {
                    if (!buttonRingTrigger.buttonShowing && !skipNextTouch && !skipTouchOnPointerUp)
                    {
                        ClickForward();
                    }

                    buttonRingTrigger.Hide(true);
                }

                skipNextTouch = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!gameController.inputEnabled)
            {
                // Mouse left button
                if (eventData.pointerId == -1)
                {
                    ClickForward();
                }

                return;
            }

            if (viewManager.currentView != CurrentViewType.Game)
            {
                return;
            }

            if (buttonRingTrigger.buttonShowing)
            {
                return;
            }

            // Stop auto/fast forward on any button or touch
            dialogueState.state = DialogueState.State.Normal;

            // Handle hyperlinks on any button or touch
            var link = dialogueBoxController.FindIntersectingLink(RealInput.mousePosition, UICameraHelper.Active);
            if (!string.IsNullOrEmpty(link))
            {
                Application.OpenURL(link);
                skipTouchOnPointerUp = true;
                return;
            }

            skipTouchOnPointerUp = false;

            // Mouse left button
            if (eventData.pointerId == -1)
            {
                ClickForward();
            }

            // Mouse right button or touch finger
            if (eventData.pointerId == -2 || eventData.pointerId >= 0)
            {
                float r = buttonRingTrigger.sectorRadius * RealScreen.fWidth / 1920 * 0.5f;
                if (RealInput.mousePosition.x > r && RealInput.mousePosition.x < RealScreen.width - r &&
                    RealInput.mousePosition.y > r && RealInput.mousePosition.y < RealScreen.height - r)
                {
                    buttonRingTrigger.ShowIfMouseMoved();
                }
            }
        }

        private void HandleInput()
        {
            if (Input.mousePresent && (
                    RealInput.mousePosition.x < 0 || RealInput.mousePosition.x > RealScreen.width ||
                    RealInput.mousePosition.y < 0 || RealInput.mousePosition.y > RealScreen.height))
            {
                // Ignore input when mouse is outside of the game window
                return;
            }

            if (buttonRingTrigger.buttonShowing)
            {
                return;
            }

            if (viewManager.currentView == CurrentViewType.Game)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (scroll > 0)
                {
                    dialogueState.state = DialogueState.State.Normal;
                    logController.Show();
                }
                else if (scroll < 0)
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

            // When user clicks, text animation should stop, independent of canAbortAnimation
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
                    Alert.Show(I18n.__("dialogue.hint.clickstopanimation"));
                    configManager.SetInt(AbortAnimationFirstShownKey, clicks + 1);
                }

                return;
            }

            if (isAnimating)
            {
                NovaAnimation.StopAll(AnimationType.PerDialogue);
                dialogueBoxController.ShowDialogueFinishIcon();
            }
        }

        #region Restoration

        public string restorableObjectName => "gameViewInput";

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