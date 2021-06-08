using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIViewTransitionBase : MonoBehaviour
    {
        protected const float CubicSlopeStart = 1.0f;
        protected const float CubicSlopeTarget = 0.0f;

        public bool useGhost;
        public bool cubic;
        public AudioClip enterSound;
        public AudioClip exitSound;

        protected RectTransform rt;
        protected Vector2 pos0, size0, scale0;
        protected CanvasGroup cg;

        private ViewManager viewManager;
        protected float delayOffset { get; private set; }
        private bool initialized;

        protected AnimationEntry.EasingFunction enterFunction =>
            cubic ? AnimationEntry.CubicEasing(CubicSlopeStart, CubicSlopeTarget) : AnimationEntry.LinearEasing();

        protected AnimationEntry.EasingFunction exitFunction =>
            cubic ? AnimationEntry.CubicEasing(CubicSlopeTarget, CubicSlopeStart) : AnimationEntry.LinearEasing();

        public abstract float enterDuration { get; }
        public abstract float exitDuration { get; }

        public virtual void Awake()
        {
            delayOffset = 0;
            viewManager = GetComponentInParent<ViewManager>();
            this.RuntimeAssert(viewManager != null, "Missing ViewManager in ancestors.");
            cg = GetComponent<CanvasGroup>();
            if (useGhost)
            {
                this.RuntimeAssert(
                    viewManager.transitionGhost != null,
                    "TransitionGhost is not set in ViewManager when using ghost."
                );
                rt = viewManager.transitionGhost.GetComponent<RectTransform>();
            }
            else
            {
                this.RuntimeAssert(cg != null, "Missing CanvasGroup when not using ghost.");
                rt = GetComponent<RectTransform>();
            }
        }

        private void CaptureToGhost()
        {
            if (viewManager.transitionGhost.texture != null)
                Destroy(viewManager.transitionGhost.texture);
            viewManager.transitionGhost.texture = ScreenCapturer.GetGameTexture();
            viewManager.transitionGhost.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }

        protected AnimationEntry GetBaseAnimationEntry()
        {
            return viewManager.uiAnimation.Do(null, delayOffset);
        }

        private void OnEnable()
        {
            if (!initialized)
            {
                ResetTransitionTarget();
            }
        }

        private void Start()
        {
            if (!initialized)
            {
                ResetTransitionTarget();
            }
        }

        public void ResetTransitionTarget()
        {
            pos0 = rt.position;
            size0 = rt.rect.size;
            scale0 = rt.localScale;
            initialized = true;
        }

        public void SetToTransitionTarget()
        {
            rt.position = pos0;
            Vector3 scale = size0.InverseScale(rt.rect.size);
            scale.x *= scale0.x;
            scale.y *= scale0.y;
            scale.z = 1;
            rt.localScale = scale;
        }

        public OpacityAnimationProperty GetOpacityAnimationProperty(float startValue, float targetValue)
        {
            if (useGhost)
                return new OpacityAnimationProperty(rt.GetComponent<RawImage>(), startValue, targetValue);
            else
                return new OpacityAnimationProperty(cg, startValue, targetValue);
        }

        protected internal virtual void OnBeforeEnter()
        {
            if (!initialized)
            {
                ResetTransitionTarget();
            }
        }

        protected abstract void OnEnter(Action onAnimationFinish);

        protected abstract void OnExit(Action onAnimationFinish);

        public void Enter(Action onAnimationFinish, float withDelay = 0)
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
                    onAnimationFinish?.Invoke();
                });
            }
            else
            {
                gameObject.SetActive(true);
                OnEnter(onAnimationFinish);
            }

            GetComponentInParent<ViewManager>().TryPlaySound(enterSound);
        }

        public void Exit(Action onAnimationFinish, float withDelay = 0)
        {
            delayOffset = withDelay;
            if (useGhost)
            {
                CaptureToGhost();
                gameObject.SetActive(false);
            }

            ResetTransitionTarget();
            OnExit(() =>
            {
                viewManager.transitionGhost.gameObject.SetActive(false);
                gameObject.SetActive(false);
                if (cg != null)
                    cg.alpha = 1;
                onAnimationFinish?.Invoke();
            });
            GetComponentInParent<ViewManager>().TryPlaySound(exitSound);
        }
    }
}