using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    public class ImageGalleryElement : MonoBehaviour, IPointerClickHandler
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

            if (group == null)
            {
                snapshot.gameObject.SetActive(false);
            }
            else
            {
                snapshot.gameObject.SetActive(true);
                snapshot.sprite = group.LoadSnapshot();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            controller.DisplayGroup(group);
        }
    }
}