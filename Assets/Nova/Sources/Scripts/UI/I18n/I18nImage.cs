using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class I18nImage : MonoBehaviour
    {
        public SerializableDictionary<SystemLanguage, Sprite> sprites;
        public SerializableDictionary<SystemLanguage, float> multipliers;

        private Image image;
        private Sprite defaultSprite;
        private RectTransform rectTransform;
        private float defaultScale;

        private void Awake()
        {
            image = GetComponent<Image>();
            this.RuntimeAssert(image != null, "Missing Image.");
            defaultSprite = image.sprite;
            rectTransform = GetComponent<RectTransform>();
            defaultScale = rectTransform.localScale.x;
        }

        private void UpdateImage()
        {
            if (sprites.TryGetValue(I18n.CurrentLocale, out var sprite))
            {
                image.sprite = sprite;
            }
            else
            {
                image.sprite = defaultSprite;
            }

            float scale = defaultScale;
            if (multipliers.TryGetValue(I18n.CurrentLocale, out var multiplier))
            {
                scale *= multiplier;
            }

            rectTransform.localScale = new Vector3(scale, scale, 1.0f);
        }

        private void OnEnable()
        {
            UpdateImage();
            I18n.LocaleChanged.AddListener(UpdateImage);
        }

        private void OnDisable()
        {
            I18n.LocaleChanged.RemoveListener(UpdateImage);
        }
    }
}
