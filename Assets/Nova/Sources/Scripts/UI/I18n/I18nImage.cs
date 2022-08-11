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
            if (sprites.ContainsKey(I18n.CurrentLocale))
            {
                image.sprite = sprites[I18n.CurrentLocale];
            }
            else
            {
                image.sprite = defaultSprite;
            }

            float scale = defaultScale;
            if (multipliers.ContainsKey(I18n.CurrentLocale))
            {
                scale *= multipliers[I18n.CurrentLocale];
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
