using UnityEngine;
using UnityEngine.EventSystems;

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
            if (Input.anyKey)
            {
                NovaAnimation.StopAll(AnimationType.UI);
            }
        }
    }
}