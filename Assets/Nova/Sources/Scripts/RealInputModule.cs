using UnityEngine.InputSystem.UI;

namespace Nova
{
    public class RealInputModule : InputSystemUIInputModule
    {
        protected override void Awake()
        {
            var realInput = gameObject.AddComponent<RealInputSystem>();
            realInput.originalInput = input;
            inputOverride = realInput;
        }
    }
}