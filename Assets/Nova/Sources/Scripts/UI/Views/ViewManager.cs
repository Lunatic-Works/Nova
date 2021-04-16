using System;
using System.Linq;
using System.Collections.Generic;
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
        public GameObject transitionInputBlocker;
        public AudioSource uiAudioSource;
        public NovaAnimation[] additionalAnimationsToBePausedWhenSwitchingView;
        public AudioController[] audiosToBePausedWhenSwitchingView;

        private IEnumerable<NovaAnimation> animationsToBePausedWhenSwitchingView =>
            GetAnimationsToBePausedWhenSwitchingView();

        private GameController gameController;

        private IEnumerable<NovaAnimation> GetAnimationsToBePausedWhenSwitchingView()
        {
            var additionalHasPerDialogue = false;
            var additionalHasPersist = false;
            foreach (var anim in additionalAnimationsToBePausedWhenSwitchingView)
            {
                if (anim == gameController.PerDialogueAnimation)
                {
                    additionalHasPerDialogue = true;
                }

                if (anim == gameController.PersistAnimation)
                {
                    additionalHasPersist = true;
                }

                yield return anim;
            }

            if (!additionalHasPersist)
            {
                yield return gameController.PersistAnimation;
            }

            if (!additionalHasPerDialogue)
            {
                yield return gameController.PerDialogueAnimation;
            }
        }

        private readonly Dictionary<Type, ViewControllerBase> controllers = new Dictionary<Type, ViewControllerBase>();
        private readonly Type[] overlayViewControllers = {typeof(NotificationViewController)};

        public GameObject dialoguePanel => GetController<DialogueBoxController>().myPanel;

        public GameObject titlePanel => GetController<TitleController>().myPanel;

        private void Awake()
        {
            currentView = CurrentViewType.UI;
            gameController = Utils.FindNovaGameController();
            uiAnimation = gameController.transform.Find("NovaAnimation/UI").GetComponent<NovaAnimation>();
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
            return controllers[typeof(T)] as T;
        }

        public CurrentViewType currentView { get; private set; }

        public void UpdateView(bool isInTransition)
        {
            var newView = isInTransition ? CurrentViewType.InTransition : QueryCurrentView();
            transitionInputBlocker.SetActive(isInTransition);
            if (currentView == newView)
                return;
            if (newView == CurrentViewType.Game && currentView != CurrentViewType.Game)
            {
                // Resume all animations
                foreach (var anim in animationsToBePausedWhenSwitchingView)
                {
                    anim.Play();
                }

                foreach (var ac in audiosToBePausedWhenSwitchingView)
                {
                    ac.UnPause();
                }
            }
            else if (currentView == CurrentViewType.Game && newView != CurrentViewType.Game)
            {
                // Pause all animations
                foreach (var anim in animationsToBePausedWhenSwitchingView)
                {
                    anim.Pause();
                }

                foreach (var ac in audiosToBePausedWhenSwitchingView)
                {
                    ac.Pause();
                }
            }

            currentView = newView;
            // Debug.LogFormat("Current view: {0}", CurrentView);
        }

        public void SwitchView<FromController, TargetController>(Action onFinish = null)
            where FromController : ViewControllerBase
            where TargetController : ViewControllerBase
        {
            GetController<FromController>().SwitchView<TargetController>(onFinish);
        }

        public void StopAllAnimations()
        {
            foreach (var anim in animationsToBePausedWhenSwitchingView)
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

            if (controllers[typeof(AlertController)].myPanel.activeSelf)
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