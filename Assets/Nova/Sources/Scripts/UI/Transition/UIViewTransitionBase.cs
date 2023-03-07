using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public abstract class UIViewTransitionBase : MonoBehaviour
    {
        protected const float CubicSlopeStart = 1f;
        protected const float CubicSlopeTarget = 0f;

        public bool useGhost;
        public bool cubic;
        public AudioClip enterSound;
        public AudioClip exitSound;

        private ViewManager viewManager;
        protected float delayOffset { get; private set; }

        protected CanvasGroup canvasGroup;
        protected RectTransform rectTransform;
        protected Vector2 pos0, size0, scale0, uiSize0;
        private RawImage rawImage;

        protected AnimationEntry.EasingFunction enterFunction => cubic
            ? AnimationEntry.CubicEasing(CubicSlopeStart, CubicSlopeTarget)
            : AnimationEntry.LinearEasing();

        protected AnimationEntry.EasingFunction exitFunction => cubic
            ? AnimationEntry.CubicEasing(CubicSlopeTarget, CubicSlopeStart)
            : AnimationEntry.LinearEasing();

        public abstract float enterDuration { get; }
        public abstract float exitDuration { get; }
        public bool inAnimation { get; protected set; }

        private bool inited;
        private bool targetInited;

        private void Init()
        {
            if (inited)
            {
                return;
            }

            viewManager = GetComponentInParent<ViewManager>();
            this.RuntimeAssert(viewManager != null, "Missing ViewManager in parents.");
            delayOffset = 0f;

            if (useGhost)
            {
                this.RuntimeAssert(viewManager.transitionGhost != null,
                    "transitionGhost is not set in ViewManager when using ghost.");
                rectTransform = viewManager.transitionGhost.GetComponent<RectTransform>();
                rawImage = rectTransform.GetComponent<RawImage>();
                this.RuntimeAssert(rawImage != null, "Missing RawImage on transitionGhost.");
            }
            else
            {
                canvasGroup = GetComponent<CanvasGroup>();
                this.RuntimeAssert(canvasGroup != null, "Missing CanvasGroup when not using ghost.");
                rectTransform = GetComponent<RectTransform>();
            }

            inited = true;
        }

        public virtual void Awake()
        {
            Init();
        }

        private void CaptureToGhost()
        {
            viewManager.transitionGhost.texture =
                ScreenCapturer.GetGameTexture(viewManager.transitionGhost.texture as RenderTexture);
            viewManager.transitionGhost.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }

        protected AnimationEntry GetBaseAnimationEntry()
        {
            return viewManager.uiAnimation.Do(null, delayOffset);
        }

        private void OnEnable()
        {
            Init();

            if (!targetInited && RealScreen.isUIInitialized)
            {
                ResetTransitionTarget();
            }
        }

        public virtual void ResetTransitionTarget()
        {
            pos0 = rectTransform.position;
            size0 = rectTransform.rect.size;
            scale0 = rectTransform.localScale;
            targetInited = true;
        }

        protected void SetToTransitionTarget()
        {
            rectTransform.position = pos0;
            Vector3 scale = size0.InverseScale(rectTransform.rect.size);
            scale.x *= scale0.x;
            scale.y *= scale0.y;
            scale.z = 1f;
            rectTransform.localScale = scale;
        }

        protected OpacityAnimationProperty GetOpacityAnimationProperty(float startValue, float targetValue)
        {
            if (useGhost)
            {
                return new OpacityAnimationProperty(rawImage, startValue, targetValue);
            }
            else
            {
                return new OpacityAnimationProperty(canvasGroup, startValue, targetValue);
            }
        }

        protected internal virtual void OnBeforeEnter()
        {
            Init();

            if (!targetInited)
            {
                ResetTransitionTarget();
            }

            inAnimation = true;
        }

        protected abstract void OnEnter(Action onFinish);

        protected abstract void OnExit(Action onFinish);

        public void Enter(Action onFinish, float withDelay = 0f)
        {
            delayOffset = withDelay;
            OnBeforeEnter();
            if (useGhost)
            {
                CaptureToGhost();
                OnEnter(() =>
                {
                    gameObject.SetActive(true);
                    viewManager.transitionGhost.gameObject.SetActive(false);
                    inAnimation = false;
                    onFinish?.Invoke();
                });
            }
            else
            {
                gameObject.SetActive(true);
                OnEnter(() =>
                {
                    inAnimation = false;
                    onFinish?.Invoke();
                });
            }

            viewManager.TryPlaySound(enterSound);
        }

        public void Exit(Action onFinish, float withDelay = 0f)
        {
            delayOffset = withDelay;
            if (useGhost)
            {
                CaptureToGhost();
                gameObject.SetActive(false);
            }

            ResetTransitionTarget();
            inAnimation = true;
            OnExit(() =>
            {
                viewManager.transitionGhost.gameObject.SetActive(false);
                gameObject.SetActive(false);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                inAnimation = false;
                onFinish?.Invoke();
            });

            viewManager.TryPlaySound(exitSound);
        }
    }
}
