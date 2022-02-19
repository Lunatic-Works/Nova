using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class RealInputModule : StandaloneInputModule
    {
        protected override void Awake()
        {
            var realInput = gameObject.AddComponent<RealInputSystem>();
            realInput.originalInput = input;
            inputOverride = realInput;
        }
    }
}