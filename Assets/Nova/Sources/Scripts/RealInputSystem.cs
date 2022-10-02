using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class RealInputSystem : BaseInput
    {
        public BaseInput originalInput;

        public override Vector2 mousePosition
        {
            get
            {
                if (Cursor.visible)
                {
                    return originalInput.mousePosition - RealScreen.offset;
                }
                else
                {
                    // Disable button hovering if the cursor is hidden
                    return Vector2.positiveInfinity;
                }
            }
        }

        public override Touch GetTouch(int index)
        {
            var touch = base.GetTouch(index);
            touch.position -= RealScreen.offset;
            return touch;
        }
    }

    public static class RealInput
    {
        // Valid even if the cursor is hidden
        // TODO: may not be needed in newer unity engine
        public static Vector3 mousePosition => Input.mousePosition - (Vector3)RealScreen.offset;
    }
}
