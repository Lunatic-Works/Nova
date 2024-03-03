using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Nova
{
    public class TransitionInputBlocker : NonDrawingGraphic, IPointerClickHandler
    {
        private int framesToDisableKeyboard;

        protected override void OnEnable()
        {
            base.OnEnable();

            framesToDisableKeyboard = 2;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            NovaAnimation.StopAll(AnimationType.UI);
        }

        private void Update()
        {
            if (framesToDisableKeyboard > 0)
            {
                --framesToDisableKeyboard;
                return;
            }

            if (Keyboard.current?.anyKey.wasReleasedThisFrame ?? false)
            {
                NovaAnimation.StopAll(AnimationType.UI);
            }
        }
    }
}
