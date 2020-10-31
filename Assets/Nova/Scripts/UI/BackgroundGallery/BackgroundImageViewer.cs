using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class BackgroundImageViewer : MonoBehaviour
    {
        public Image image;
        public Vector2 defaultImageSize = new Vector2(1920, 1080);

        public Text indexLabel;
        public Text scaleLabel;

        private BackgroundGroup group;
        private int index;
        private float scale;

        public void SetBackgroundGroup(BackgroundGroup group)
        {
            this.group = group;
            index = 0;
            scale = 1.0f;
            Refresh();
        }

        private void Refresh()
        {
            if (index >= group.entries.Count)
            {
                image.sprite = null;
                return;
            }

            var sprite = image.sprite = AssetLoader.Load<Sprite>(group.entries[index].resourcePath);
            if (sprite != null)
            {
                image.rectTransform.sizeDelta = new Vector2(sprite.texture.width, sprite.texture.height);
            }
            else
            {
                image.rectTransform.sizeDelta = defaultImageSize;
            }

            image.rectTransform.localScale = new Vector3(scale, scale, 1.0f);

            indexLabel.text = $"{index + 1}/{group.entries.Count}";
            scaleLabel.text = $"{scale:0.0}x";
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ZoomIn()
        {
            scale += 0.1f;
            Refresh();
        }

        public void ZoomOut()
        {
            scale = Mathf.Max(0.1f, scale - 0.1f);
            Refresh();
        }

        public void NextImage()
        {
            index++;
            if (index >= group.entries.Count) index--;
            Refresh();
        }

        public void PreviousImage()
        {
            index--;
            if (index < 0) index = 0;
            Refresh();
        }
    }
}