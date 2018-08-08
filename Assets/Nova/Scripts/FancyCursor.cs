using UnityEngine;

namespace Nova
{
    public class FancyCursor : MonoBehaviour
    {
        public Texture2D cursorTexture;
        public CursorMode cursorMode = CursorMode.Auto;
        public Vector2 hotSpot = Vector2.zero;
        public float hideAfterSeconds = 10.0f;

        private void OnEnable()
        {
            Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
            lastCursorPosition = Input.mousePosition;
        }

        private void OnDisable()
        {
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
        }

        private Vector3 lastCursorPosition;
        private float idleTime;

        private void Update()
        {
            var currentCursorPosition = Input.mousePosition;
            if (currentCursorPosition != lastCursorPosition)
            {
                // show cursor, reset idle time
                Cursor.visible = true;
                lastCursorPosition = currentCursorPosition;
                idleTime = 0;
                return;
            }

            if (Cursor.visible)
            {
                idleTime += Time.deltaTime;
                if (idleTime > hideAfterSeconds)
                {
                    idleTime = 0;
                    Cursor.visible = false;
                }
            }
        }
    }
}