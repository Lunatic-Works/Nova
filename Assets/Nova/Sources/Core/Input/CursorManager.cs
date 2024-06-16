// It depends on RealInput.mousePosition and TouchFix
// RealInput.pointerPosition depends on it
//
// Do not get Cursor.visible when testing if using keyboard,
// because it can be changed by Unity Editor

using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Nova
{
    /// <summary>
    /// Hides cursor and clears selection.
    /// </summary>
    public class CursorManager : MonoBehaviour
    {
        public static bool AlwaysDeselect => Application.isMobilePlatform;
        public static bool IsMouseOrTouchPressed =>
            (Mouse.current?.allControls.OfType<ButtonControl>().Any(control => control.isPressed) ?? false) ||
            TouchFix.IsTouchPressed;

        // Used to disable RealInput.pointerPosition
        public static bool UsingKeyboard;

        public static bool MovedLastFrame;

        [SerializeField] private float hideAfterSeconds = 5.0f;

        private bool inited;
        private EventSystem eventSystem;

        private Vector2 lastMousePosition;
        private float idleTime;

        // If we call it in Start, EventSystem.current can be uninitialized
        private void Init()
        {
            if (inited)
            {
                return;
            }

            eventSystem = EventSystem.current;
            lastMousePosition = RealInput.mousePosition;

            inited = true;
        }

        private void Update()
        {
            Init();

            // Clear selection on mobile platforms
            if (AlwaysDeselect)
            {
                eventSystem.SetSelectedGameObject(null);
                return;
            }

            // Show cursor and clear selection when mouse moves or clicks
            var mousePosition = RealInput.mousePosition;
            MovedLastFrame = !mousePosition.ApproxEquals(lastMousePosition, 1f);
            if (MovedLastFrame || IsMouseOrTouchPressed)
            {
                Cursor.visible = !TouchFix.IsTouchPressed;
                UsingKeyboard = false;
                lastMousePosition = mousePosition;
                idleTime = 0.0f;
                eventSystem.SetSelectedGameObject(null);
                return;
            }

            if (Keyboard.current?.anyKey.wasPressedThisFrame ?? false)
            {
                Cursor.visible = false;
                UsingKeyboard = true;
                return;
            }

            // Hide cursor after an idle time
            // Do not disable RealInput.pointerPosition
            if (hideAfterSeconds > 0.0f && !UsingKeyboard)
            {
                idleTime += Time.unscaledDeltaTime;
                if (idleTime > hideAfterSeconds)
                {
                    Cursor.visible = false;
                }
            }
        }
    }
}
