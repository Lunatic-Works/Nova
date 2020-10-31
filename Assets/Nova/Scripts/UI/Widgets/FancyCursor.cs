using UnityEngine;

namespace Nova
{
    public class FancyCursor : MonoBehaviour
    {
        public Texture2D cursorTexture;
        public Vector2 hotSpot = Vector2.zero;
        public bool autoHideAfterDuration;
        public float hideAfterSeconds = 10.0f;

        private Vector3 lastCursorPosition;
        private float idleTime;

        private void OnEnable()
        {
            if (cursorTexture != null)
            {
                Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
            }

            lastCursorPosition = RealInput.mousePosition;
        }

        private void OnDisable()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void Update()
        {
            if (!autoHideAfterDuration) return;

            Vector3 currentCursorPosition = RealInput.mousePosition;
            if (currentCursorPosition != lastCursorPosition)
            {
                // show cursor, reset idle time
                Cursor.visible = true;
                lastCursorPosition = currentCursorPosition;
                idleTime = 0.0f;
                return;
            }

            if (Cursor.visible)
            {
                idleTime += Time.deltaTime;
                if (idleTime > hideAfterSeconds)
                {
                    Cursor.visible = false;
                    idleTime = 0.0f;
                }
            }
        }
    }
}