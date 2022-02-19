using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class UISound : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler,
        IPointerEnterHandler
    {
        public AudioClip mouseDown;
        public AudioClip mouseUp;
        public AudioClip mouseEnter;
        public AudioClip mouseExit;
        public AudioClip mouseInsideLoop;

        private ViewManager viewManager;

        private void Awake()
        {
            viewManager = Utils.FindViewManager();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Only mouse left button or touch plays sound
            if (eventData.pointerId < -1)
            {
                return;
            }

            viewManager.TryPlaySound(mouseDown);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Only mouse left button or touch plays sound
            if (eventData.pointerId < -1)
            {
                return;
            }

            if (mouseInsideLoop != null)
            {
                viewManager.TryStopSound();
            }

            viewManager.TryPlaySound(mouseUp);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (mouseInsideLoop != null)
            {
                viewManager.TryPlaySound(mouseInsideLoop);
            }
            else
            {
                viewManager.TryPlaySound(mouseEnter);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (mouseInsideLoop != null)
            {
                viewManager.TryStopSound();
            }
            else
            {
                viewManager.TryPlaySound(mouseExit);
            }
        }
    }
}