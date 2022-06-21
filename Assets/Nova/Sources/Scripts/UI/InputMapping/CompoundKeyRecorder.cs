using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Nova
{
    using KeyStatus = Dictionary<AbstractKey, bool>;

    public class CompoundKeyRecorder : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Key[] AllowedKeys =
        {
            Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J, Key.K, Key.L, Key.M, Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T, Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
            Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
            Key.Space, Key.Enter, Key.Tab, Key.Backquote, Key.Quote, Key.Semicolon, Key.Comma, Key.Period, Key.Slash, Key.Backslash, Key.LeftBracket, Key.RightBracket, Key.Minus, Key.Equals, Key.Backspace, Key.Delete, Key.Insert, Key.Home, Key.End, Key.PageUp, Key.PageDown, Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow,
            Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9,
            Key.NumLock, Key.CapsLock, Key.ScrollLock, Key.Pause, Key.PrintScreen, Key.NumpadMultiply, Key.NumpadDivide, Key.NumpadEnter, Key.NumpadEquals, Key.NumpadPeriod, Key.NumpadPlus, Key.NumpadMinus, Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9,
            Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12,
        };
        private static IEnumerable<ButtonControl> GetAllowedMouseButtons()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                yield break;
            }
            yield return mouse.middleButton;
            yield return mouse.forwardButton;
            yield return mouse.backButton;
        }

        private static IEnumerable<ButtonControl> GetAllowedGamepadButtons()
        {
            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                yield break;
            }
            yield return gamepad.buttonEast;
            yield return gamepad.buttonSouth;
            yield return gamepad.buttonWest;
            yield return gamepad.buttonNorth;
            yield return gamepad.leftShoulder;
            yield return gamepad.rightShoulder;
            yield return gamepad.leftTrigger;
            yield return gamepad.rightTrigger;
            yield return gamepad.startButton;
            yield return gamepad.selectButton;
            yield return gamepad.leftStickButton;
            yield return gamepad.rightStickButton;
            yield return gamepad.dpad.up;
            yield return gamepad.dpad.down;
            yield return gamepad.dpad.left;
            yield return gamepad.dpad.right;
        }

        public RecordPopupController popupController;

        private InputMappingController controller;
        private InputAction action => controller.currentAction;
        public bool isRebinding { get; private set; }
        private bool isCtrl;
        private bool isAlt;
        private bool isWin;
        private bool isShift;
        private readonly List<InputControl> bindingResult = new List<InputControl>();
        private KeyStatus keyEnabled;

        private static string GetGeneralPath(InputControl control)
        {
            var path = control.path;
            path = path.Replace("/Gamepad/", "<Gamepad>/");
            path = path.Replace("/Keyboard/", "<Keyboard>/");
            path = path.Replace("/Mouse/", "<Mouse>/");
            path = path.Replace("/Joystick/", "<Joystick>/");
            return path;
        }

        private void UpdateBinding()
        {
            action.ChangeBinding(entry.bindingData.startIndex).Erase();
            if (bindingResult.Count == 1)
            {
                action.AddBinding(GetGeneralPath(bindingResult[0]));
            }
            else if (bindingResult.Count == 2)
            {
                action.AddCompositeBinding("OneModifier")
                    .With("Modifier", GetGeneralPath(bindingResult[0]))
                    .With("Binding", GetGeneralPath(bindingResult[1]));
            }
            else if (bindingResult.Count == 3)
            {
                action.AddCompositeBinding("TwoModifiers")
                    .With("Modifier1", GetGeneralPath(bindingResult[0]))
                    .With("Modifier2", GetGeneralPath(bindingResult[1]))
                    .With("Binding", GetGeneralPath(bindingResult[2]));
            }
            entry.RefreshDisplay();
        }

        private void AddControl(InputControl control)
        {
            if (bindingResult.Count < 3
                && !bindingResult.Any(input => input.path == control.path))
            {
                bindingResult.Add(control);
                UpdateBinding();
            }
        }

        private void OnEnable()
        {
            popupController.entry = entry;
            popupController.Show();
            bindingResult.Clear();
            isRebinding = true;
            isCtrl = isAlt = isWin = isShift = false;
            keyEnabled = controller.inputManager.GetEnabledState();
            controller.inputManager.SetEnableGroup(AbstractKeyGroup.None);
        }

        private void OnDisable()
        {
            isRebinding = false;
            isCtrl = isAlt = isWin = isShift = false;
            controller.inputManager.SetEnabledState(keyEnabled);

            popupController.Hide();

            if (entry != null)
            {
                entry.FinishModify();

                if (bindingResult.Count == 0 && string.IsNullOrWhiteSpace(entry.bindingData.displayString))
                {
                    entry.Delete();
                }
                else
                {
                    var duplicate = controller.bindingData
                        .FirstOrDefault(data => data != entry.bindingData && data.SameButtonAs(entry.bindingData));

                    if (duplicate != null)
                    {
                        entry.Delete();
                    }
                    else
                    {
                        controller.ResolveDuplicate();
                    }
                }
            }

            entry = null;
        }

        public void Init(InputMappingController controller)
        {
            this.controller = controller;
            gameObject.SetActive(false);
        }

        private InputMappingListEntry entry;

        public void BeginRecording(InputMappingListEntry entry)
        {
            this.entry = entry;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (entry == null) return;
            // Keyboard input
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (!isCtrl && keyboard.ctrlKey.wasPressedThisFrame)
                {
                    isCtrl = true;
                    AddControl(keyboard.ctrlKey);
                }
                if (!isAlt && keyboard.altKey.wasPressedThisFrame)
                {
                    isAlt = true;
                    AddControl(keyboard.altKey);
                }
                if (!isWin && keyboard.leftWindowsKey.wasPressedThisFrame || keyboard.rightWindowsKey.wasPressedThisFrame)
                {
                    isWin = true;
                    AddControl(keyboard.leftWindowsKey);
                }
                if (!isShift && keyboard.shiftKey.wasPressedThisFrame)
                {
                    isShift = true;
                    AddControl(keyboard.shiftKey);
                }
                foreach (var key in AllowedKeys)
                {
                    var keyControl = keyboard[key];
                    if (keyControl.wasPressedThisFrame)
                    {
                        AddControl(keyControl);
                        gameObject.SetActive(false);
                        return;
                    }
                }
            }

            // Mouse input
            var mouseButton = GetAllowedMouseButtons().FirstOrDefault(button => button.wasPressedThisFrame);
            if (mouseButton != null)
            {
                AddControl(mouseButton);
                gameObject.SetActive(false);
                return;
            }

            // Gamepad input
            foreach (var control in GetAllowedGamepadButtons().Where(control => control.wasPressedThisFrame))
            {
                AddControl(control);
                gameObject.SetActive(false);
                return;
            }

            // If any previously pressed key is released, finalize the binding
            if (bindingResult.Count > 0)
            {
                if (bindingResult.Any(input => !input.IsPressed()))
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left
                || eventData.button == PointerEventData.InputButton.Right)
            {
                gameObject.SetActive(false);
            }
        }
    }
}