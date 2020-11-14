using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public partial class DialogueBoxController
    {
        private bool _fastForwardHotKeyHolding;

        private bool fastForwardHotKeyHolding
        {
            get => _fastForwardHotKeyHolding;
            set
            {
                if (_fastForwardHotKeyHolding == value) return;
                _fastForwardHotKeyHolding = value;
                state = value ? DialogueBoxState.FastForward : DialogueBoxState.Normal;
            }
        }

        private void HandleShortCutInGameView()
        {
            if (inputMapper.GetKeyUp(AbstractKey.Auto))
            {
                state = DialogueBoxState.Auto;
            }

            if (inputMapper.GetKeyUp(AbstractKey.Save))
            {
                viewManager.GetController<SaveViewController>().ShowSave();
            }

            if (inputMapper.GetKeyUp(AbstractKey.Load))
            {
                viewManager.GetController<SaveViewController>().ShowLoad();
            }

            if (inputMapper.GetKeyUp(AbstractKey.QuickSave))
            {
                viewManager.GetController<SaveViewController>().QuickSaveBookmark();
            }

            if (inputMapper.GetKeyUp(AbstractKey.QuickLoad))
            {
                viewManager.GetController<SaveViewController>().QuickLoadBookmark();
            }

            if (inputMapper.GetKeyUp(AbstractKey.ToggleDialogue))
            {
                Hide();
            }

            if (inputMapper.GetKeyUp(AbstractKey.StepForward))
            {
                ClickForward();
            }

            if (inputMapper.GetKeyUp(AbstractKey.ShowLog))
            {
                viewManager.GetController<LogController>().Show();
            }

            fastForwardHotKeyHolding = inputMapper.GetKey(AbstractKey.FastForward);
        }

        private void HandleShortCutWhenDialogueHidden()
        {
            if (inputMapper.GetKeyUp(AbstractKey.ToggleDialogue))
            {
                Show();
            }
        }

#if UNITY_EDITOR

        private void HandleEditorOnlyShortCut()
        {
            if (viewManager.currentView == CurrentViewType.Game)
            {
                if (Input.GetKeyUp(KeyCode.LeftArrow))
                {
                    state = DialogueBoxState.Normal;
                    try
                    {
                        gameState.SeekBackStep(1, out var nodeName, out var dialogueIndex);
                        gameState.MoveBackTo(nodeName, dialogueIndex, "");
                    }
                    catch
                    {
                        // ignored
                    }
                }

                if (Input.GetKeyUp(KeyCode.Backspace))
                {
                    JumpChapter(0);
                }

                if (Input.GetKeyUp(KeyCode.LeftBracket))
                {
                    JumpChapter(-1);
                }

                if (Input.GetKeyUp(KeyCode.RightBracket))
                {
                    JumpChapter(1);
                }

                if (Input.GetKeyUp(KeyCode.P))
                {
                    useThemedBox = true;
                    textColor = Color.white;
                }
            }
        }

#endif

        private void HandleShortCut()
        {
            if (inputMapper.GetKeyUp(AbstractKey.ToggleFullscreen))
            {
                GameRenderManager.SwitchFullScreen();
            }

            if (viewManager.currentView == CurrentViewType.Game)
            {
                HandleShortCutInGameView();
            }

            if (viewManager.currentView == CurrentViewType.DialogueHidden)
            {
                HandleShortCutWhenDialogueHidden();
            }

            // some editor only short cuts
#if UNITY_EDITOR
            HandleEditorOnlyShortCut();
#endif
        }

        [HideInInspector] public RightButtonAction rightButtonAction;

        private bool skipNextTouch = false;
        private bool skipTouchOnPointerUp = false;

        public void OnPointerUp(PointerEventData eventData)
        {
            if (gameController.disableInput)
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
                Show();
                return;
            }
            else if (view != CurrentViewType.Game)
            {
                return;
            }

            if (buttonRingTrigger.buttonShowing)
            {
                buttonRingTrigger.Hide(!buttonRingTrigger.holdOpen || eventData.pointerId != -2);
                return;
            }

            // Mouse right button
            if (eventData.pointerId == -2 || Input.touchCount == 2)
            {
                if (!buttonRingTrigger.buttonShowing)
                {
                    buttonRingTrigger.NoShowIfMouseMoved();
                    if (rightButtonAction == RightButtonAction.HideDialoguePanel)
                    {
                        Hide();
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

                if (Input.touchCount == 2)
                {
                    skipNextTouch = true;
                }
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
            if (gameController.disableInput)
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

            // Stop auto/fast forward on any button or torch
            state = DialogueBoxState.Normal;

            // Handle hyperlinks on any button or torch
            Camera uiCamera = UICameraHelper.Active;
            foreach (var dec in dialogueText.dialogueEntryControllers)
            {
                string link = dec.FindIntersectingLink(RealInput.mousePosition, uiCamera);
                if (link != "")
                {
                    Application.OpenURL(link);
                    skipTouchOnPointerUp = true;
                    return;
                }
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
                    RealInput.mousePosition.y < 0 || RealInput.mousePosition.y > RealScreen.height)
            )
            {
                // Ignore input when mouse is outside of the game window
                return;
            }

            if (viewManager.currentView == CurrentViewType.Game)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (scroll > 0)
                {
                    state = DialogueBoxState.Normal;
                    logController.Show();
                }
                else if (scroll < 0)
                {
                    ClickForward();
                }
            }
        }

        [HideInInspector] public bool clickForwardAbility = true;
        [HideInInspector] public bool abortAnimationAbility = true;
        [HideInInspector] public bool scriptAbortAnimationAbility = true;
        [HideInInspector] public bool onlyFastForwardRead = true;

        public void ClickForward()
        {
            if (!clickForwardAbility)
            {
                return;
            }

            state = DialogueBoxState.Normal;

            if (!IsAnimating && !textAnimationIsPlaying)
            {
                NextPageOrStep();
                return;
            }

            // When user clicks, text animation should stop, independent of AbortAnimationAbility
            if (textAnimationIsPlaying)
            {
                StopTextAnimation();
            }

            if (!abortAnimationAbility || !scriptAbortAnimationAbility)
            {
                return;
            }

            if (IsAnimating)
            {
                NovaAnimation.StopAll(AnimationType.PerDialogue);

                dialogueFinished.SetActive(true);
            }
        }
    }
}