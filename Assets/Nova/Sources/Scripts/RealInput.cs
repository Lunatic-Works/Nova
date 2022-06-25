using UnityEngine;
using UnityEngine.InputSystem;

namespace Nova
{
    public static class RealInput
    {
        // Valid even if the cursor is hidden
        public static Vector2 mousePosition
        {
            get
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    return mouse.position.ReadValue() - RealScreen.offset;
                }
                else
                {
                    return Vector2.positiveInfinity;
                }
            }
        }
    }
}