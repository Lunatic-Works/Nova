using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nova
{
    using ImageUnlockInfo = SerializableHashSet<string>;

    public class ImageViewer : MonoBehaviour, IPointerClickHandler
    {
        public CompositeSpriteMerger merger;
        public Camera renderCamera;
        public RawImage image;
        public Vector2 defaultImageSize = new Vector2(1920, 1080);
        public float maxScale = 2.0f;
        public float scaleStep = 0.1f;

        private RenderTexture renderTexture;
        private ImageGroup group;
        private ImageUnlockInfo unlockInfo;
        private int index;
        private float scale;

        private void ResetImage()
        {
            var entry = group.entries[index];
            if (entry.composite)
            {
                if (renderTexture != null)
                {
                    Destroy(renderTexture);
                }

                var sprites = CompositeSpriteController.LoadSprites(entry.resourcePath, entry.poseString);
                if (!sprites.Any() || sprites.Contains(null))
                {
                    return;
                }

                renderTexture = merger.RenderToTexture(sprites, renderCamera);
                image.texture = renderTexture;
            }
            else
            {
                var sprite = AssetLoader.Load<Sprite>(entry.resourcePath);
                image.texture = sprite.texture;
            }

            image.rectTransform.sizeDelta = new Vector2(image.texture.width, image.texture.height);
        }

        private void Refresh(bool resetImage)
        {
            if (resetImage)
            {
                ResetImage();
            }

            float baseScale = Mathf.Max(defaultImageSize.x / image.texture.width,
                defaultImageSize.y / image.texture.height);
            image.rectTransform.localScale = new Vector3(baseScale * scale, baseScale * scale, 1.0f);
        }

        public void Show(ImageGroup group, ImageUnlockInfo unlockInfo)
        {
            gameObject.SetActive(true);
            this.group = group;
            this.unlockInfo = unlockInfo;
            index = ImageGalleryController.GetNextUnlockedImage(group, unlockInfo, -1);
            scale = 1.0f;
            Refresh(true);
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
                Refresh(true);
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
                Refresh(true);
            }
            else
            {
                Hide();
            }
        }

        public void ZoomIn()
        {
            scale = Mathf.Min(scale + scaleStep, maxScale);
            Refresh(false);
        }

        public void ZoomOut()
        {
            scale = Mathf.Max(scale - scaleStep, 1.0f);
            Refresh(false);
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
