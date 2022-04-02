using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIViewTransitionBase : MonoBehaviour
    {
        protected const float CubicSlopeStart = 1f;
        protected const float CubicSlopeTarget = 0f;

        public bool useGhost;
        public bool cubic;
        public AudioClip enterSound;
        public AudioClip exitSound;

        protected RectTransform rt;
        protected Vector2 pos0, size0, scale0;
        protected CanvasGroup cg;
        private RawImage rawImage;

        private ViewManager viewManager;
        protected float delayOffset { get; private set; }
        private bool inited;

        protected AnimationEntry.EasingFunction enterFunction => cubic
            ? AnimationEntry.CubicEasing(CubicSlopeStart, CubicSlopeTarget)
            : AnimationEntry.LinearEasing();

        protected AnimationEntry.EasingFunction exitFunction => cubic
            ? AnimationEntry.CubicEasing(CubicSlopeTarget, CubicSlopeStart)
            : AnimationEntry.LinearEasing();

        public abstract float enterDuration { get; }
        public abstract float exitDuration { get; }

        public virtual void Awake()
        {
            delayOffset = 0f;
            viewManager = GetComponentInParent<ViewManager>();
            this.RuntimeAssert(viewManager != null, "Missing ViewManager in parents.");
            cg = GetComponent<CanvasGroup>();
            if (useGhost)
            {
                this.RuntimeAssert(viewManager.transitionGhost != null,
                    "TransitionGhost is not set in ViewManager when using ghost.");
                rt = viewManager.transitionGhost.GetComponent<RectTransform>();
                rawImage = rt.GetComponent<RawImage>();
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
            {
                Destroy(viewManager.transitionGhost.texture);
            }

            viewManager.transitionGhost.texture = ScreenCapturer.GetGameTexture();
            viewManager.transitionGhost.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }

        protected AnimationEntry GetBaseAnimationEntry()
        {
            return viewManager.uiAnimation.Do(null, delayOffset);
        }

        private void Start()
        {
            if (!inited)
            {
                ResetTransitionTarget();
            }
        }

        private void OnEnable()
        {
            if (!inited)
            {
                ResetTransitionTarget();
            }
        }

        public virtual void ResetTransitionTarget()
        {
            pos0 = rt.position;
            size0 = rt.rect.size;
            scale0 = rt.localScale;
            inited = true;
        }

        public void SetToTransitionTarget()
        {
            rt.position = pos0;
            Vector3 scale = size0.InverseScale(rt.rect.size);
            scale.x *= scale0.x;
            scale.y *= scale0.y;
            scale.z = 1f;
            rt.localScale = scale;
        }

        public OpacityAnimationProperty GetOpacityAnimationProperty(float startValue, float targetValue)
        {
            if (useGhost)
            {
                return new OpacityAnimationProperty(rawImage, startValue, targetValue);
            }
            else
            {
                return new OpacityAnimationProperty(cg, startValue, targetValue);
            }
        }

        protected internal virtual void OnBeforeEnter()
        {
            if (!inited)
            {
                ResetTransitionTarget();
            }
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
                    onFinish?.Invoke();
                });
            }
            else
            {
                gameObject.SetActive(true);
                OnEnter(onFinish);
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
            OnExit(() =>
            {
                viewManager.transitionGhost.gameObject.SetActive(false);
                gameObject.SetActive(false);
                if (cg != null)
                {
                    cg.alpha = 1f;
                }

                onFinish?.Invoke();
            });

            viewManager.TryPlaySound(exitSound);
        }
    }
}