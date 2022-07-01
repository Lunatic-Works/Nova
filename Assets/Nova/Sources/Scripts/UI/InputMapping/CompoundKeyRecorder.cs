using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Nova
{
    using KeyStatus = Dictionary<AbstractKey, bool>;

    public class CompoundKeyRecorder : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Regex PathPattern = new Regex(@"^\/?[^\/]*\/", RegexOptions.Compiled);

        private static readonly Key[] AllowedKeys =
        {
            Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J, Key.K, Key.L, Key.M,
            Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T, Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
            Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
            Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12,
            Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
            Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9,
            Key.NumLock, Key.NumpadEquals, Key.NumpadDivide, Key.NumpadMultiply,
            Key.NumpadMinus, Key.NumpadPlus, Key.NumpadEnter, Key.NumpadPeriod,
            Key.Escape, Key.Backquote, Key.Minus, Key.Equals, Key.Backslash, Key.Backspace,
            Key.Tab, Key.CapsLock, Key.LeftBracket, Key.RightBracket, Key.Enter,
            Key.Semicolon, Key.Quote, Key.Comma, Key.Period, Key.Slash, Key.Space,
            Key.Insert, Key.Delete, Key.Home, Key.End, Key.PageUp, Key.PageDown,
            Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow,
            Key.PrintScreen, Key.ScrollLock, Key.Pause,
            Key.ContextMenu, Key.OEM1, Key.OEM2, Key.OEM3, Key.OEM4, Key.OEM5,
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

        private readonly List<InputControl> boundControls = new List<InputControl>();
        private readonly KeyStatus enabledState = new KeyStatus();

        /// <summary>
        /// Gets general paths from input control.
        /// The specific path associated with the input control cannot be used to determine
        /// duplicate shortcuts or easily generate display strings.
        /// </summary>
        private static string GetGeneralPath(InputControl control)
        {
            var path = control.path;
            if (control.device is Mouse)
            {
                path = PathPattern.Replace(path, "<Mouse>/");
            }
            else if (control.device is Gamepad)
            {
                path = PathPattern.Replace(path, "<Gamepad>/");
            }
            else if (control.device is Keyboard)
            {
                path = PathPattern.Replace(path, "<Keyboard>/");
            }

            return path;
        }

        /// <summary>
        /// Removes the original binding and adds the new recorded binding.
        /// </summary>
        private void ApplyBinding()
        {
            if (entry != null)
            {
                action.ChangeBinding(entry.compositeBinding.startIndex).Erase();
            }

            if (boundControls.Count == 1)
            {
                action.AddBinding(GetGeneralPath(boundControls[0]));
            }
            else if (boundControls.Count == 2)
            {
                action.AddCompositeBinding("OneModifier")
                    .With("Modifier", GetGeneralPath(boundControls[0]))
                    .With("Binding", GetGeneralPath(boundControls[1]));
            }
            else if (boundControls.Count == 3)
            {
                action.AddCompositeBinding("TwoModifiers")
                    .With("Modifier1", GetGeneralPath(boundControls[0]))
                    .With("Modifier2", GetGeneralPath(boundControls[1]))
                    .With("Binding", GetGeneralPath(boundControls[2]));
            }
        }

        private void AddControl(InputControl control)
        {
            // Allow at most 2 modifiers and 1 binding.
            if (boundControls.Count < 3 && boundControls.All(input => input.path != control.path))
            {
                boundControls.Add(control);
            }
        }

        private void OnEnable()
        {
            isRebinding = true;
            boundControls.Clear();
            popupController.controls = boundControls;
            popupController.Show();
            isCtrl = isAlt = isWin = isShift = false;
            controller.inputManager.GetEnabledState(enabledState);
            controller.inputManager.SetEnableGroup(AbstractKeyGroup.None);
        }

        private void OnDisable()
        {
            isRebinding = false;
            isCtrl = isAlt = isWin = isShift = false;
            controller.inputManager.SetEnabledState(enabledState);

            popupController.Hide();
            entry?.FinishModify();

            if (boundControls.Count == 0)
            {
                entry?.Remove();
                entry = null;
                return;
            }

            ApplyBinding();
            InputMappingController.ResolveDuplicateForAction(action);
            controller.ResolveDuplicate();
            entry = null;
        }

        public void Init(InputMappingController controller)
        {
            this.controller = controller;
            gameObject.SetActive(false);
        }

        private InputBindingEntry entry;

        /// <summary>
        /// Modify an entry or add a new entry.
        /// </summary>
        /// <param name="entry">The entry to modify. If null, adds a new entry.</param>
        public void BeginRecording(InputBindingEntry entry)
        {
            this.entry = entry;
            gameObject.SetActive(true);
        }

        private void Update()
        {
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

                if (!isWin && keyboard.leftWindowsKey.wasPressedThisFrame ||
                    keyboard.rightWindowsKey.wasPressedThisFrame)
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
            if (boundControls.Count > 0)
            {
                if (boundControls.Any(input => !input.IsPressed()))
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left ||
                eventData.button == PointerEventData.InputButton.Right)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
