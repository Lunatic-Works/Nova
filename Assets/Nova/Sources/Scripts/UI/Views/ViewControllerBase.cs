using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nova
{
    public abstract class ViewControllerBase : MonoBehaviour
    {
        public GameObject myPanel;
        public ViewManager viewManager { get; private set; }

        protected List<UIViewTransitionBase> transitions;
        protected InputManager inputManager;

        private bool inited;

        protected virtual bool Init()
        {
            if (inited)
            {
                return true;
            }

            this.RuntimeAssert(myPanel != null, "Missing myPanel.");
            transitions = myPanel.GetComponents<UIViewTransitionBase>().ToList();
            viewManager = GetComponentInParent<ViewManager>();
            this.RuntimeAssert(viewManager != null, "Missing ViewManager in parents.");
            viewManager.SetController(this);
            inputManager = Utils.FindNovaController().InputManager;

            inited = true;
            return false;
        }

        protected virtual void Awake()
        {
            Init();
        }

        protected virtual void Start()
        {
            myPanel.SetActive(true);
            ForceRebuildLayoutAndResetTransitionTarget();
            myPanel.SetActive(false);
        }

        protected virtual void ForceRebuildLayoutAndResetTransitionTarget()
        {
            // Rebuild all layouts the hard way
            foreach (var layout in GetComponentsInChildren<LayoutGroup>())
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
            }

            if (RealScreen.isUIInitialized)
            {
                foreach (var transition in GetComponentsInChildren<UIViewTransitionBase>())
                {
                    transition.ResetTransitionTarget();
                }
            }
        }

        public void Show()
        {
            Show(null);
        }

        public void Hide()
        {
            Hide(null);
        }

        public virtual void Show(Action onFinish)
        {
            if (myPanel.activeSelf)
            {
                onFinish?.Invoke();
                return;
            }

            myPanel.SetActive(true);
            var transition = transitions.FirstOrDefault(t => t.enabled);
            if (transition != null)
            {
                viewManager.UpdateView(true);
                transition.Enter(() =>
                {
                    viewManager.UpdateView(false);
                    onFinish?.Invoke();
                });
            }
            else
            {
                viewManager.UpdateView(false);
                onFinish?.Invoke();
            }
        }

        public virtual void Hide(Action onFinish)
        {
            if (!myPanel.activeSelf)
            {
                onFinish?.Invoke();
                return;
            }

            var transition = transitions.FirstOrDefault(t => t.enabled);
            if (transition != null)
            {
                viewManager.UpdateView(true);
                transition.Exit(() =>
                {
                    OnHideComplete();
                    onFinish?.Invoke();
                });
            }
            else
            {
                OnHideComplete();
                onFinish?.Invoke();
            }
        }

        protected virtual void OnDestroy()
        {
            viewManager.UnsetController(this);
        }

        protected virtual void OnHideComplete()
        {
            myPanel.SetActive(false);
            viewManager.UpdateView(false);
        }

        protected virtual void BackHide()
        {
            Hide();
        }

        protected virtual void OnActivatedUpdate()
        {
            if (inputManager.IsTriggered(AbstractKey.LeaveView)
                || Mouse.current?.rightButton.wasReleasedThisFrame == true)
            {
                BackHide();
            }
        }

        protected virtual void Update()
        {
            if (myPanel.activeSelf && viewManager.currentView == CurrentViewType.UI)
            {
                OnActivatedUpdate();
            }
        }
    }
}
