using UnityEngine;

namespace Nova
{
    public class ButtonRingTrigger : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Canvas currentCanvas;
        private RectTransform backgroundBlur;
        private ButtonRing buttonRing;

        public bool buttonShowing { get; private set; }
        public bool holdOpen { get; private set; }

        public float sectorRadius => buttonRing.sectorRadius;

        private Vector2? lastPointerPosition;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            currentCanvas = GetComponentInParent<Canvas>();
            backgroundBlur = transform.Find("BackgroundBlur").GetComponent<RectTransform>();
            buttonRing = GetComponentInChildren<ButtonRing>();
        }

        private void ForceHideChildren()
        {
            buttonShowing = false;
            backgroundBlur.gameObject.SetActive(false);
            buttonRing.gameObject.SetActive(false);
        }

        public void Show(bool holdOpen)
        {
            if (buttonShowing)
            {
                return;
            }

            this.holdOpen = holdOpen;

            AdjustAnchorPosition();
            buttonShowing = true;
            backgroundBlur.gameObject.SetActive(true);
            buttonRing.gameObject.SetActive(true);

            if (holdOpen)
            {
                buttonRing.BeginEntryAnimation();
            }
        }

        public void Hide(bool triggerAction)
        {
            if (!buttonShowing)
            {
                return;
            }

            holdOpen = false;

            lastPointerPosition = null;
            buttonShowing = false;
            if (!triggerAction)
            {
                buttonRing.SuppressNextAction();
            }

            backgroundBlur.gameObject.SetActive(false);
            buttonRing.gameObject.SetActive(false);
        }

        private void AdjustAnchorPosition()
        {
            var targetPosition = lastPointerPosition ?? RealInput.pointerPosition;
            rectTransform.anchoredPosition = currentCanvas.ScreenToCanvasPosition(targetPosition);
            Vector2 v = currentCanvas.ViewportToCanvasPosition(Vector3.one) * 2.0f;
            backgroundBlur.offsetMin = -v;
            backgroundBlur.offsetMax = v;
        }

        public void ShowIfPointerMoved()
        {
            lastPointerPosition = RealInput.pointerPosition;
        }

        public void NoShowIfPointerMoved()
        {
            lastPointerPosition = null;
        }

        private bool isFirstCalled = true;

        // Have to use late update
        private void LateUpdate()
        {
            if (!buttonShowing &&
                lastPointerPosition != null &&
                (RealInput.pointerPosition - lastPointerPosition.Value).magnitude > sectorRadius * 0.5f)
            {
                Show(false);
            }

            // Wait for all sectors to initialize
            if (!isFirstCalled) return;
            ForceHideChildren();
            isFirstCalled = false;
        }
    }
}
