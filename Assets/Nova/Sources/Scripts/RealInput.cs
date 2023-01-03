using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Nova
{
    [RequireComponent(typeof(InputSystemUIInputModule))]
    public class RealInput : MonoBehaviour
    {
        private static RealInput Current;
        private static bool focused = true;

        // Valid even if the cursor is hidden
        public static Vector2 mousePosition
        {
            get
            {
                if (Mouse.current == null || !focused)
                {
                    return Vector2.positiveInfinity;
                }

                return Mouse.current.position.ReadValue() - RealScreen.offset;
            }
        }

        public static Vector2 pointerPosition
        {
            get
            {
                if (Current?.action == null || !focused)
                {
                    return Vector2.positiveInfinity;
                }

                return Current.action.ReadValue<Vector2>();
            }
        }

        private InputAction action;

        private void Awake()
        {
            Current = this;
            action = GetComponent<InputSystemUIInputModule>().point.action;
        }

        private void OnApplicationFocus(bool focus)
        {
            focused= focus;
        }
    }
}
