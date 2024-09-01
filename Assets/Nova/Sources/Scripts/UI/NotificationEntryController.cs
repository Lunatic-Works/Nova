using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class NotificationEntryController : MonoBehaviour
    {
        [SerializeField] private Text text;
        public RectTransform rectTransform;
        public UIViewTransitionBase transition;

        [SerializeField] private float smoothTime = 0.1f;

        [HideInInspector] public float targetY;
        private float velocity;
        private Vector3 panelPos0;

        private Dictionary<SystemLanguage, string> content;

        public void Init(Dictionary<SystemLanguage, string> content)
        {
            this.content = content;
            UpdateText();

            velocity = 0f;
            panelPos0 = transition.transform.localPosition;
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

        private void Update()
        {
            var pos = transform.localPosition;
            if (pos.y < targetY)
            {
                pos.y = Mathf.SmoothDamp(pos.y, targetY, ref velocity, smoothTime);
                transform.localPosition = pos;
            }
        }

        // UI transition can change localPosition.y, so here we change it back
        private void LateUpdate()
        {
            var pos = transition.transform.localPosition;
            pos.y = panelPos0.y;
            transition.transform.localPosition = pos;
        }
    }
}
