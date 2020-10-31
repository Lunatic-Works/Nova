using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class UISound : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
    {
        public AudioClip mouseDown;
        public AudioClip mouseUp;
        public AudioClip mouseEnter;
        public AudioClip mouseExit;
        public AudioClip mouseInsideLoop;

        public void OnPointerDown(PointerEventData eventData)
        {
            // Only mouse left button or torch plays sound
            if (eventData.pointerId < -1)
            {
                return;
            }

            GetComponentInParent<ViewManager>().TryPlaySound(mouseDown);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Only mouse left button or torch plays sound
            if (eventData.pointerId < -1)
            {
                return;
            }

            if (mouseInsideLoop != null)
                GetComponentInParent<ViewManager>().TryStopSound();
            GetComponentInParent<ViewManager>().TryPlaySound(mouseUp);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (mouseInsideLoop != null)
                GetComponentInParent<ViewManager>().TryPlaySound(mouseInsideLoop);
            else
                GetComponentInParent<ViewManager>().TryPlaySound(mouseEnter);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (mouseInsideLoop != null)
                GetComponentInParent<ViewManager>().TryStopSound();
            else
                GetComponentInParent<ViewManager>().TryPlaySound(mouseExit);
        }
    }
}