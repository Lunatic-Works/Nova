using UnityEngine;

namespace Nova
{
    public class RealInput : MonoBehaviour
    {
        public static bool Focused { get; private set; }

        // Valid even if the cursor is hidden
        public static Vector3 mousePosition
        {
            get
            {
                if (!Focused)
                {
                    return Vector3.positiveInfinity;
                }

                return Input.mousePosition - (Vector3)RealScreen.offset;
            }
        }

        private void Awake()
        {
            Focused = true;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            Focused = hasFocus;
        }
    }
}
