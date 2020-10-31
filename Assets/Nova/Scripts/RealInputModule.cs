using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class RealInputModule : StandaloneInputModule
    {
        protected override void Awake()
        {
            Input.multiTouchEnabled = false;

            var realInput = gameObject.AddComponent<RealInputSystem>();
            realInput.originalInput = input;
            m_InputOverride = realInput;
        }
    }
}