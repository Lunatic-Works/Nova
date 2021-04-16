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

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            currentCanvas = GetComponentInParent<Canvas>();
            buttonRing = GetComponentInChildren<ButtonRing>();
            backgroundBlur = transform.Find("BackgroundBlur").GetComponent<RectTransform>();
        }

        private void ForceHideChildren()
        {
            buttonShowing = false;
            backgroundBlur.gameObject.SetActive(false);
            buttonRing.gameObject.SetActive(false);
        }

        public void Show(bool holdOpen)
        {
            NoShowIfMouseMoved();

            if (buttonShowing)
            {
                return;
            }

            this.holdOpen = holdOpen;
            AdjustAnchorPosition();

            buttonShowing = true;
            buttonRing.gameObject.SetActive(true);
            backgroundBlur.gameObject.SetActive(true);

            if (this.holdOpen)
            {
                buttonRing.BeginEntryAnimation();
            }
        }

        public void Hide(bool triggerAction)
        {
            NoShowIfMouseMoved();

            if (!buttonShowing)
            {
                return;
            }

            holdOpen = false;

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
            var targetPosition = currentCanvas.ScreenToCanvasPosition(RealInput.mousePosition);
            rectTransform.anchoredPosition = targetPosition;
            Vector2 v = currentCanvas.ViewportToCanvasPosition(Vector3.one) * 2;
            backgroundBlur.offsetMin = -v;
            backgroundBlur.offsetMax = v;
        }

        private Vector3? lastMousePosition = null;

        public void ShowIfMouseMoved()
        {
            lastMousePosition = RealInput.mousePosition;
        }

        public void NoShowIfMouseMoved()
        {
            lastMousePosition = null;
        }

        private bool isFirstCalled = true;

        private void LateUpdate()
        {
            if (lastMousePosition != null)
            {
                if ((RealInput.mousePosition - lastMousePosition).Value.magnitude > 20f)
                {
                    lastMousePosition = null;
                    Show(false);
                }
            }

            // have to use late update
            // wait for all background sectors fully initialized
            if (!isFirstCalled) return;
            ForceHideChildren();
            isFirstCalled = false;
        }
    }
}