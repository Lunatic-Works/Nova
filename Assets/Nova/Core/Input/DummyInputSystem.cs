using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    // A dummy system does nothing
    public class DummyInputSystem : BaseInput
    {
        public override string compositionString => "";

        public override IMECompositionMode imeCompositionMode
        {
            get => IMECompositionMode.Off;
            set { }
        }

        public override Vector2 compositionCursorPos
        {
            get => Vector2.zero;
            set { }
        }

        public override bool mousePresent => false;

        public override bool GetMouseButtonDown(int button)
        {
            return false;
        }

        public override bool GetMouseButtonUp(int button)
        {
            return false;
        }

        public override bool GetMouseButton(int button)
        {
            return false;
        }

        public override Vector2 mousePosition => Vector2.zero;

        public override Vector2 mouseScrollDelta => Vector2.zero;

        public override bool touchSupported => false;

        public override int touchCount => 0;

        public override Touch GetTouch(int index)
        {
            return new Touch();
        }

        public override float GetAxisRaw(string axisName)
        {
            return 0;
        }

        public override bool GetButtonDown(string buttonName)
        {
            return false;
        }
    }
}