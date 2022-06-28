// TODO: check on all platforms that when a touch is detected, the platform will hide the cursor

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
        public float hideAfterSeconds = 5.0f;

        private Vector2 lastCursorPosition;
        private float idleTime;

        private void Awake()
        {
            lastCursorPosition = RealInput.mousePosition;
        }

        private void Update()
        {
            var eventSystem = EventSystem.current;

            // Clear selection on mobile platforms
            if (Application.isMobilePlatform)
            {
                eventSystem.SetSelectedGameObject(null);
                return;
            }

            // Show cursor and clear selection when mouse moves or clicks
            var cursorPosition = RealInput.mousePosition;
            if (cursorPosition != lastCursorPosition ||
                Mouse.current?.allControls.OfType<ButtonControl>().Any(control => control.isPressed) == true)
            {
                Cursor.visible = true;
                lastCursorPosition = cursorPosition;
                idleTime = 0.0f;
                eventSystem.SetSelectedGameObject(null);
                return;
            }

            // Hide cursor after an idle time
            if (hideAfterSeconds > 0.0f && Cursor.visible)
            {
                idleTime += Time.deltaTime;
                if (idleTime > hideAfterSeconds)
                {
                    Cursor.visible = false;
                }
            }
        }
    }
}