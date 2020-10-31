using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    public class BackgroundGalleryElement : MonoBehaviour, IPointerClickHandler
    {
        public Image snapshot;

        private BackgroundGroup group;
        private BackgroundGalleryController controller;

        private void Awake()
        {
            controller = GetComponentInParent<BackgroundGalleryController>();
        }

        public void SetGroup(BackgroundGroup group)
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