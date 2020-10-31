using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class BGMGalleryMusicProgressBarKnob : MonoBehaviour, IPointerDownHandler
    {
        private BGMGalleryMusicProgressBar bar;

        private void Awake()
        {
            bar = GetComponentInParent<BGMGalleryMusicProgressBar>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            bar.isDragged = true;
        }

        private void Update()
        {
            // IPointerUpHandler has undesired behaviour
            if (bar.isDragged && Input.GetMouseButtonUp(0))
            {
                bar.isDragged = false;
            }
        }
    }
}