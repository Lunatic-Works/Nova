using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Nova
{
    [RequireComponent(typeof(InputSystemUIInputModule))]
    public class RealInput : MonoBehaviour
    {
        private static RealInput Current;

        // Valid even if the cursor is hidden
        public static Vector2 mousePosition
        {
            get
            {
                if (Mouse.current == null)
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
                if (Current?.action == null)
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
    }
}
