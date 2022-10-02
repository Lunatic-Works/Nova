using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class NotificationEntryController : MonoBehaviour
    {
        public Text text;
        public RectTransform rectTransform;
        public UIViewTransitionBase transition;

        private Dictionary<SystemLanguage, string> content;

        public void Init(Dictionary<SystemLanguage, string> content)
        {
            this.content = content;
            UpdateText();
        }

        private void UpdateText()
        {
            if (content == null)
            {
                return;
            }

            text.text = I18n.__(content);
        }

        private void OnEnable()
        {
            UpdateText();
            I18n.LocaleChanged.AddListener(UpdateText);
        }

        private void OnDisable()
        {
            I18n.LocaleChanged.RemoveListener(UpdateText);
        }
    }
}
