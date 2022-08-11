using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class I18nImage : MonoBehaviour
    {
        public List<LocaleSpritePair> sprites;
        public SerializableDictionary<SystemLanguage, float> multipliers;

        private Image image;
        private Sprite defaultSprite;
        private RectTransform rectTransform;
        private float scale;

        private void Awake()
        {
            image = GetComponent<Image>();
            this.RuntimeAssert(image != null, "Missing Image.");
            defaultSprite = image.sprite;
            rectTransform = GetComponent<RectTransform>();
            scale = rectTransform.localScale.x;
        }

        private void UpdateImage()
        {
            bool found = false;
            foreach (var pair in sprites)
            {
                if (pair.locale == I18n.CurrentLocale)
                {
                    image.sprite = pair.sprite;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                image.sprite = defaultSprite;
            }

            float _scale = scale;
            if (multipliers.ContainsKey(I18n.CurrentLocale))
            {
                _scale *= multipliers[I18n.CurrentLocale];
            }

            rectTransform.localScale = new Vector3(_scale, _scale, 1.0f);
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
