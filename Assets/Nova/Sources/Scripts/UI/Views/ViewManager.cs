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
        DialogueHidden,
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

        private readonly Dictionary<Type, ViewControllerBase> controllers = new Dictionary<Type, ViewControllerBase>();
        private readonly Type[] overlayViewControllers = {typeof(NotificationController)};

        public GameObject dialoguePanel => GetController<DialogueBoxController>().myPanel;
        public GameObject titlePanel => GetController<TitleController>().myPanel;
        public GameObject alertPanel => GetController<AlertController>().myPanel;

        private void Awake()
        {
            currentView = CurrentViewType.UI;
            novaController = Utils.FindNovaController();
            uiAnimation = novaController.transform.Find("NovaAnimation/UI").GetComponent<NovaAnimation>();
            screenCapturer = GetComponent<ScreenCapturer>();
            this.RuntimeAssert(screenCapturer != null, "Missing ScreenCapturer.");
        }

        public void SetController(ViewControllerBase controller)
        {
            controllers[controller.GetType()] = controller;
        }

        public void UnsetController(ViewControllerBase controller)
        {
            controllers.Remove(controller.GetType());
        }

        public T GetController<T>() where T : ViewControllerBase
        {
            var t = typeof(T);
            if (controllers.ContainsKey(t))
            {
                return controllers[t] as T;
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
            }

            currentView = newView;
            // Debug.Log($"Current view: {CurrentView}");
        }

        public void SwitchView<FromController, TargetController>(Action onFinish = null)
            where FromController : ViewControllerBase
            where TargetController : ViewControllerBase
        {
            GetController<FromController>().SwitchView<TargetController>(onFinish);
        }

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

            if (alertPanel.activeSelf)
            {
                return CurrentViewType.Alert;
            }

            var activeNonGameControllerCount = controllers.Values.Count(c =>
                overlayViewControllers.All(t => !t.IsInstanceOfType(c)) &&
                !(c is DialogueBoxController) &&
                c.myPanel.activeSelf
            );
            if (activeNonGameControllerCount == 0)
            {
                if (dialoguePanel.activeSelf)
                {
                    return CurrentViewType.Game;
                }
                else
                {
                    return CurrentViewType.DialogueHidden;
                }
            }

            return CurrentViewType.UI;
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
        public static void SwitchView<TargetController>(this ViewControllerBase controller, Action onFinish = null)
            where TargetController : ViewControllerBase
        {
            controller.Hide(() =>
                controller.viewManager.GetController<TargetController>().Show(onFinish)
            );
        }
    }
}
