// TODO: check on all platforms that when a touch is detected, the platform will hide the cursor

using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    /// <summary>
    /// Hides cursor and clears selection.
    /// </summary>
    public class CursorManager : MonoBehaviour
    {
        public float hideAfterSeconds = 5.0f;

        private EventSystem eventSystem;
        private Vector3 lastMousePosition;
        private float idleTime;

        private void Start()
        {
            eventSystem = EventSystem.current;
            lastMousePosition = RealInput.mousePosition;
        }

        private void Update()
        {
            // Clear selection on mobile platforms
            if (Application.isMobilePlatform)
            {
                eventSystem.SetSelectedGameObject(null);
                return;
            }

            // Show cursor and clear selection when mouse moves or clicks
            var mousePosition = RealInput.mousePosition;
            if (mousePosition != lastMousePosition ||
                Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
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
