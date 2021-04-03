using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    using ImageUnlockInfo = SerializableHashSet<string>;

    public class ImageViewer : MonoBehaviour, IPointerClickHandler
    {
        public Image image;
        public Vector2 defaultImageSize = new Vector2(1920, 1080);
        public float maxScale = 2.0f;
        public float scaleStep = 0.1f;

        private ImageGroup group;
        private ImageUnlockInfo unlockInfo;
        private int index;
        private float scale;

        private void Refresh()
        {
            var sprite = AssetLoader.Load<Sprite>(group.entries[index].resourcePath);
            image.sprite = sprite;
            image.rectTransform.sizeDelta = new Vector2(sprite.texture.width, sprite.texture.height);
            float baseScale = Mathf.Max(defaultImageSize.x / sprite.texture.width,
                defaultImageSize.y / sprite.texture.height);
            image.rectTransform.localScale = new Vector3(baseScale * scale, baseScale * scale, 1.0f);
        }

        public void Show(ImageGroup group, ImageUnlockInfo unlockInfo)
        {
            gameObject.SetActive(true);
            this.group = group;
            this.unlockInfo = unlockInfo;
            index = ImageGalleryController.GetNextUnlockedImage(group, unlockInfo, -1);
            scale = 1.0f;
            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void PreviousImage()
        {
            index = ImageGalleryController.GetPreviousUnlockedImage(group, unlockInfo, index);
            if (index >= 0)
            {
                Refresh();
            }
            else
            {
                Hide();
            }
        }

        public void NextImage()
        {
            index = ImageGalleryController.GetNextUnlockedImage(group, unlockInfo, index);
            if (index >= 0)
            {
                Refresh();
            }
            else
            {
                Hide();
            }
        }

        public void ZoomIn()
        {
            scale = Mathf.Min(scale + scaleStep, maxScale);
            Refresh();
        }

        public void ZoomOut()
        {
            scale = Mathf.Max(scale - scaleStep, 1.0f);
            Refresh();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                NextImage();
            }
        }
    }
}