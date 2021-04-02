using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    public class ImageGalleryEntry : MonoBehaviour, IPointerClickHandler
    {
        public Image snapshot;

        private ImageGroup group;
        private ImageGalleryController controller;

        private void Awake()
        {
            controller = GetComponentInParent<ImageGalleryController>();
        }

        public void SetGroup(ImageGroup group)
        {
            this.group = group;
            snapshot.sprite = group.LoadSnapshot();
        }

        public void SetGroup(Sprite sprite)
        {
            group = null;
            snapshot.sprite = sprite;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (group != null)
            {
                controller.DisplayGroup(group);
            }
        }
    }
}