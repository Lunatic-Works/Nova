// TODO: check on all platforms that when a touch is detected, the platform will hide the cursor

using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class CursorManager : MonoBehaviour
    {
        public float hideAfterSeconds = 5.0f;

        private Vector3 lastCursorPosition;
        private float idleTime;

        private void Awake()
        {
            lastCursorPosition = RealInput.mousePosition;
        }

        private void Update()
        {
            var eventSystem = EventSystem.current;

            // Disable keyboard navigation on mobile platforms
            if (Application.isMobilePlatform)
            {
                eventSystem.SetSelectedGameObject(null);
                return;
            }

            // Show cursor and disable keyboard navigation when mouse moves or clicks
            var cursorPosition = RealInput.mousePosition;
            if (cursorPosition != lastCursorPosition ||
                Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
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
