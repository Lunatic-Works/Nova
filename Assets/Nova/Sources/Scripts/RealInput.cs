using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Nova
{
    public static class RealInput
    {
        // Valid even if the cursor is hidden
        // TODO: may not be needed in newer unity engine
        // TODO(Input System): Confirm if this is the intended behavior
        public static Vector2 mousePosition => (Mouse.current?.position.ReadValue() ?? Vector2.positiveInfinity) - RealScreen.offset;
    }
}