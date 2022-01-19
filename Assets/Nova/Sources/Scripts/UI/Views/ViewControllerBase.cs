using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public abstract class ViewControllerBase : MonoBehaviour
    {
        public GameObject myPanel;
        public ViewManager viewManager { get; private set; }

        protected List<UIViewTransitionBase> transitions;
        protected InputMapper inputMapper;

        private bool inited;

        protected void Init()
        {
            if (inited)
            {
                return;
            }

            this.RuntimeAssert(myPanel != null, "MyPanel is not set.");
            transitions = myPanel.GetComponents<UIViewTransitionBase>().ToList();
            viewManager = GetComponentInParent<ViewManager>();
            this.RuntimeAssert(viewManager != null, "Missing ViewManager in parents.");
            viewManager.SetController(this);
            inputMapper = Utils.FindNovaGameController().InputMapper;

            inited = true;
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

        protected void ForceRebuildLayoutAndResetTransitionTarget()
        {
            // Rebuild all layout the hard way
            foreach (var layout in GetComponentsInChildren<LayoutGroup>())
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
            }

            foreach (var transition in GetComponentsInChildren<UIViewTransitionBase>())
            {
                transition.ResetTransitionTarget();
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
                transition.Enter(() =>
                {
                    viewManager.UpdateView(false);
                    onFinish?.Invoke();
                });
                viewManager.UpdateView(transition != null);
            }
            else
            {
                viewManager.UpdateView(transition != null);
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

        private bool backHotKeyHolding;

        protected virtual void BackHide()
        {
            Hide();
        }

        protected virtual void OnActivatedUpdate()
        {
            // TODO: elegant way to handle a key down and a key up event
            if (inputMapper.GetKey(AbstractKey.LeaveView) || Input.GetMouseButton(1))
            {
                backHotKeyHolding = true;
            }
            else
            {
                if (backHotKeyHolding)
                {
                    backHotKeyHolding = false;
                    BackHide();
                }
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