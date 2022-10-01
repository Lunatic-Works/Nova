using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Nova
{
    public class TransitionInputBlocker : NonDrawingGraphic, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            NovaAnimation.StopAll(AnimationType.UI);
        }

        private void Update()
        {
            if (Keyboard.current?.anyKey.isPressed == true)
            {
                NovaAnimation.StopAll(AnimationType.UI);
            }
        }
    }
}
