using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public enum CurrentViewType
    {
        UI,
        Game,
        InTransition,
        Alert
    }

    [RequireComponent(typeof(ScreenCapturer))]
    public class ViewManager : MonoBehaviour
    {
        [HideInInspector] public NovaAnimation uiAnimation;
        [HideInInspector] public ScreenCapturer screenCapturer;

        public RawImage transitionGhost;
        [SerializeField] private GameObject transitionInputBlocker;
        [SerializeField] private AudioSource uiAudioSource;

        // Pause some animations and audios when switching the view
        [SerializeField] private List<NovaAnimation> animationsToPause;
        [SerializeField] private List<AudioController> audiosToPause;

        private NovaController novaController;

        // animationsToPause + novaController.PerDialogueAnimation + novaController.HoldingAnimation
        private IEnumerable<NovaAnimation> allAnimationsToPause => GetAllAnimationsToPause();

        private IEnumerable<NovaAnimation> GetAllAnimationsToPause()
        {
            var hasPerDialogue = false;
            var hasHolding = false;
            foreach (var anim in animationsToPause)
            {
                if (anim == novaController.PerDialogueAnimation)
                {
                    hasPerDialogue = true;
                }

                if (anim == novaController.HoldingAnimation)
                {
                    hasHolding = true;
                }

                yield return anim;
            }

            if (!hasPerDialogue)
            {
                yield return novaController.PerDialogueAnimation;
            }

            if (!hasHolding)
            {
                yield return novaController.HoldingAnimation;
            }
        }

        private readonly Dictionary<Type, IViewController> controllers = new Dictionary<Type, IViewController>();
        private readonly Type[] overlayViewControllers = {typeof(NotificationController)};

        private void Awake()
        {
            currentView = CurrentViewType.UI;
            novaController = Utils.FindNovaController();
            uiAnimation = novaController.UIAnimation;
            screenCapturer = GetComponent<ScreenCapturer>();
            this.RuntimeAssert(screenCapturer != null, "Missing ScreenCapturer.");
        }

        public void SetController(IViewController controller)
        {
            controllers[controller.GetType()] = controller;
        }

        public void UnsetController(IViewController controller)
        {
            controllers.Remove(controller.GetType());
        }

        public T GetController<T>() where T : class, IViewController
        {
            var t = typeof(T);
            if (controllers.TryGetValue(t, out var value))
            {
                return value as T;
            }

            var controller = GetComponentInChildren<T>();
            this.RuntimeAssert(controller != null, $"Cannot find {t}.");
            controllers[t] = controller;
            return controller;
        }

        public CurrentViewType currentView { get; private set; }

        public void UpdateView(bool isInTransition)
        {
            var newView = isInTransition ? CurrentViewType.InTransition : QueryCurrentView();
            transitionInputBlocker.SetActive(isInTransition);
            if (currentView == newView)
            {
                return;
            }

            if (newView == CurrentViewType.Game && currentView != CurrentViewType.Game)
            {
                // Resume all animations
                foreach (var anim in allAnimationsToPause)
                {
                    anim.Play();
                }

                foreach (var ac in audiosToPause)
                {
                    ac.UnPause();
                }
            }
            else if (currentView == CurrentViewType.Game && newView != CurrentViewType.Game)
            {
                // Pause all animations
                foreach (var anim in allAnimationsToPause)
                {
                    anim.Pause();
                }

                foreach (var ac in audiosToPause)
                {
                    ac.Pause();
                }

                GameCharacterController.StopVoiceAll();
            }

            currentView = newView;
            // Debug.Log($"Current view: {CurrentView}");
        }

        public void SwitchView<FromController, TargetController>(Action onFinish = null)
            where FromController : class, IViewController
            where TargetController : class, IViewController
        {
            var from = GetController<FromController>();
            var target = GetController<TargetController>();
            from.Hide(() => target.Show(onFinish));
        }

        // By default equivalent to NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Holding) and used in UI
        // while NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text) is usually used in game view
        public void StopAllAnimations()
        {
            foreach (var anim in allAnimationsToPause)
            {
                anim.Stop();
            }
        }

        private CurrentViewType QueryCurrentView()
        {
            if (transitionGhost.gameObject.activeSelf)
            {
                return CurrentViewType.InTransition;
            }

            if (GetController<AlertController>().active)
            {
                return CurrentViewType.Alert;
            }

            var hasUI = controllers.Values.Any(c =>
                overlayViewControllers.All(t => !t.IsInstanceOfType(c)) &&
                !(c is GameViewController) &&
                c.active
            );

            return hasUI ? CurrentViewType.UI : CurrentViewType.Game;
        }

        public void TryPlaySound(AudioClip clip)
        {
            if (uiAudioSource != null && clip != null)
            {
                uiAudioSource.clip = clip;
                uiAudioSource.Play();
            }
        }

        public void TryStopSound()
        {
            if (uiAudioSource != null)
            {
                uiAudioSource.Stop();
            }
        }
    }

    public static class ViewHelper
    {
        public static void SwitchView<TargetController>(this IViewController controller, Action onFinish = null)
            where TargetController : class, IViewController
        {
            controller.Hide(() => controller.viewManager.GetController<TargetController>().Show(onFinish));
        }

        public static void Hide(this IPanelController panel, Action onFinish)
        {
            panel.Hide(true, onFinish);
        }

        public static void Show(this IPanelController panel, Action onFinish)
        {
            panel.Show(true, onFinish);
        }

        public static void Hide(this IPanelController panel)
        {
            panel.Hide(true, null);
        }

        public static void Show(this IPanelController panel)
        {
            panel.Show(true, null);
        }

        public static void ShowImmediate(this IPanelController panel, Action onFinish)
        {
            panel.Show(false, onFinish);
        }

        public static void HideImmediate(this IPanelController panel, Action onFinish)
        {
            panel.Hide(false, onFinish);
        }

        public static void ShowImmediate(this IPanelController panel)
        {
            panel.Show(false, null);
        }

        public static void HideImmediate(this IPanelController panel)
        {
            panel.Hide(false, null);
        }
    }
}
