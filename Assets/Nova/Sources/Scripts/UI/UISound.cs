using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

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
        private Selectable selectable;

        private bool interactable => selectable == null || selectable.interactable;

        private void Awake()
        {
            viewManager = Utils.FindViewManager();
            selectable = GetComponent<Selectable>();
        }

        public void OnPointerDown(PointerEventData _eventData)
        {
            if (!interactable)
            {
                return;
            }

            var eventData = (ExtendedPointerEventData)_eventData;
            // Only mouse left button or touch plays sound
            if (eventData != null && !TouchFix.IsTouch(eventData) &&
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            viewManager.TryPlaySound(mouseDown);
        }

        public void OnPointerUp(PointerEventData _eventData)
        {
            if (!interactable)
            {
                return;
            }

            var eventData = (ExtendedPointerEventData)_eventData;
            // Only mouse left button or touch plays sound
            if (eventData != null && !TouchFix.IsTouch(eventData) &&
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (mouseInsideLoop != null)
            {
                viewManager.TryStopSound();
            }

            viewManager.TryPlaySound(mouseUp);
        }

        public void OnPointerEnter(PointerEventData _eventData)
        {
            if (!interactable)
            {
                return;
            }

            var eventData = (ExtendedPointerEventData)_eventData;
            if (TouchFix.IsTouch(eventData))
            {
                return;
            }

            if (mouseInsideLoop != null)
            {
                viewManager.TryPlaySound(mouseInsideLoop);
            }
            else
            {
                viewManager.TryPlaySound(mouseEnter);
            }
        }

        public void OnPointerExit(PointerEventData _eventData)
        {
            if (!interactable)
            {
                return;
            }

            var eventData = (ExtendedPointerEventData)_eventData;
            if (TouchFix.IsTouch(eventData))
            {
                return;
            }

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
