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
            if (Application.isMobilePlatform)
            {
                eventSystem.SetSelectedGameObject(null);
                return;
            }

            // Show cursor and clear selection when mouse moves or clicks
            var mousePosition = RealInput.mousePosition;
            if (mousePosition != lastMousePosition ||
                Mouse.current.allControls.OfType<ButtonControl>().Any(control => control.isPressed))
            {
                Cursor.visible = true;
                lastMousePosition = mousePosition;
                idleTime = 0.0f;
                eventSystem.SetSelectedGameObject(null);
                return;
            }

            // Hide cursor after an idle time
            if (hideAfterSeconds > 0.0f && Cursor.visible)
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
