using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Nova
{
    public class MusicGalleryProgressBarKnob : MonoBehaviour, IPointerDownHandler
    {
        private MusicGalleryProgressBar bar;

        private void Awake()
        {
            bar = GetComponentInParent<MusicGalleryProgressBar>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            bar.isDragged = true;
        }

        private void Update()
        {
            // IPointerUpHandler has undesired behaviour
            if (bar.isDragged && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                bar.isDragged = false;
            }
        }
    }
}