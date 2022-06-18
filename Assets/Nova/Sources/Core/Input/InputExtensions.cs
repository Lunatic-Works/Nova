using UnityEngine.InputSystem;

namespace Nova
{
    public static class InputExtensions
    {
        public static InputActionAsset Clone(this InputActionAsset actionAsset)
        {
            return InputActionAsset.FromJson(actionAsset.ToJson());
        }
    }
}