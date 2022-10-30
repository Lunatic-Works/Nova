using System;
using System.Collections;
using UnityEngine;

namespace Nova
{
    public class NotificationController : ViewControllerBase
    {
        [SerializeField] private NotificationEntryController notificationPrefab;
        [SerializeField] private float notificationTimePerChar = 0.1f;
        [SerializeField] private float notificationTimeOffset = 1f;
        [SerializeField] private float notificationDropSpeed = 500f;

        private float placeholdingDeadItemCountPercentage
        {
            get => (layoutGroupBasePos.y - layoutGroupTransform.position.y) *
                   (RealScreen.uiSize.y / UICameraHelper.Active.orthographicSize * 0.5f);
            set
            {
                Vector3 newPos = layoutGroupTransform.position;
                newPos.y = layoutGroupBasePos.y -
                           value / (RealScreen.uiSize.y / UICameraHelper.Active.orthographicSize * 0.5f);
                layoutGroupTransform.position = newPos;
            }
        }

        private Vector2 layoutGroupBasePos;
        private RectTransform layoutGroupTransform;

        protected override void Awake()
        {
            base.Awake();
            this.RuntimeAssert(notificationPrefab != null, "Missing notificationPrefab.");
        }

        protected override void Start()
        {
            base.Start();
            myPanel.SetActive(true);
            layoutGroupTransform = myPanel.GetComponent<RectTransform>();
            layoutGroupBasePos = layoutGroupTransform.position;
        }

        private IEnumerator NotificationFadeOut(NotificationEntryController notification, float timeout,
            Action onFinish)
        {
            yield return new WaitForSeconds(timeout);
            var transition = notification.transition;

            // TODO: there is a NovaAnimation in NotificationItem, and this Action is in that NovaAnimation.
            // Destroy(notification) will call NovaAnimation.OnDisable() -> NovaAnimation.Stop() -> this Action.
            // That is a strange call stack.
            bool onFinishInvoked = false;
            transition.ResetTransitionTarget();
            transition.Exit(() =>
            {
                if (onFinishInvoked) return;
                onFinishInvoked = true;
                onFinish?.Invoke();

                placeholdingDeadItemCountPercentage += notification.rectTransform.rect.size.y;
                Destroy(notification);
            });
        }

        public void Notify(AlertParameters param)
        {
            if (!param.lite)
            {
                return;
            }

            var notification = Instantiate(notificationPrefab, myPanel.transform);
            notification.Init(param.content);
            float timeout = notificationTimePerChar * I18n.__(param.content).Length + notificationTimeOffset;
            ForceRebuildLayoutAndResetTransitionTarget();
            var transition = notification.transition;
            transition.Enter(() => StartCoroutine(NotificationFadeOut(notification, timeout, param.onCancel)));
        }

        protected override void Update()
        {
            var delta = placeholdingDeadItemCountPercentage;
            if (delta < float.Epsilon)
            {
                placeholdingDeadItemCountPercentage = 0f;
            }
            else
            {
                placeholdingDeadItemCountPercentage = delta - Time.deltaTime * notificationDropSpeed;
            }
        }
    }
}
