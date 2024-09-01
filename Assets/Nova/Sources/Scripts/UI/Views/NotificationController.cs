using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class NotificationController : ViewControllerBase
    {
        [SerializeField] private NotificationEntryController notificationPrefab;
        [SerializeField] private float notificationTimePerChar = 0.1f;
        [SerializeField] private float notificationTimeOffset = 1f;

        private Transform panelTransform;

        protected override void Awake()
        {
            base.Awake();

            this.RuntimeAssert(notificationPrefab != null, "Missing notificationPrefab.");
            panelTransform = myPanel.transform;
        }

        protected override void Start()
        {
            base.Start();

            myPanel.SetActive(true);
        }

        private IEnumerator NotificationFadeOut(NotificationEntryController notification, float timeout,
            Action onFinish)
        {
            yield return new WaitForSeconds(timeout);

            var height = notification.rectTransform.rect.height;
            var childCount = panelTransform.childCount;
            for (var i = notification.transform.GetSiblingIndex() + 1; i < childCount; ++i)
            {
                panelTransform.GetChild(i).GetComponent<NotificationEntryController>().targetY += height;
            }

            notification.transition.Exit(() =>
            {
                Destroy(notification.gameObject);
                onFinish?.Invoke();
            });
        }

        public void Notify(AlertParameters param)
        {
            if (!param.lite)
            {
                return;
            }

            var notification = Instantiate(notificationPrefab, panelTransform);
            var trans = notification.transform;
            trans.SetAsLastSibling();
            var pos = trans.localPosition;
            if (panelTransform.childCount > 1)
            {
                var child = panelTransform.GetChild(panelTransform.childCount - 2);
                var lastNotification = child.GetComponent<NotificationEntryController>();
                var lastHeight = lastNotification.rectTransform.rect.height;
                pos.y = child.localPosition.y - lastHeight;
                trans.localPosition = pos;

                notification.targetY = lastNotification.targetY - lastHeight;
            }
            else
            {
                notification.targetY = pos.y;
            }

            notification.Init(param.content);
            // Fix text glitch at the first frame
            LayoutRebuilder.ForceRebuildLayoutImmediate(notification.rectTransform);

            float timeout = notificationTimePerChar * I18n.__(param.content).Length + notificationTimeOffset;
            notification.transition.Enter(
                () => StartCoroutine(NotificationFadeOut(notification, timeout, param.onCancel))
            );
        }

        protected override void Update()
        {
            // Do nothing
        }
    }
}
