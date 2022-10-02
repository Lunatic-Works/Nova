using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class UISound : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler,
        IPointerExitHandler
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
            if (!(Utils.IsTouch(eventData) || Utils.IsLeftButton(eventData)))
            {
                return;
            }

            viewManager.TryPlaySound(mouseDown);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Only mouse left button or touch plays sound
            if (!(Utils.IsTouch(eventData) || Utils.IsLeftButton(eventData)))
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
            // TODO: Is the loop correct?
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
