using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class PanelController : MonoBehaviour, IPanelController
    {
        [SerializeField] protected GameObject myPanel;

        protected List<UIViewTransitionBase> transitions;
        protected bool inited;
        public ViewManager viewManager { get; private set; }

        protected virtual bool Init()
        {
            if (inited)
            {
                return true;
            }

            this.RuntimeAssert(myPanel != null, "Missing myPanel.");
            transitions = myPanel.GetComponents<UIViewTransitionBase>().ToList();

            viewManager = Utils.FindViewManager();

            inited = true;
            return false;
        }

        protected virtual void Awake()
        {
            Init();
        }

        public bool active => myPanel.activeSelf;

        protected virtual void OnTransitionBegin() { }

        protected virtual void OnShowFinish() { }

        // this function calls before myPanel inactive
        protected virtual void OnHideComplete() { }

        // this function calls after myPanel inactive but before onFinish
        protected virtual void OnHideFinish() { }

        public virtual void Show(bool doTransition, Action onFinish)
        {
            if (active)
            {
                onFinish?.Invoke();
                return;
            }

            Action onFinishAll = () =>
            {
                OnShowFinish();
                onFinish?.Invoke();
            };
            myPanel.SetActive(true);
            var transition = transitions.FirstOrDefault(t => t.enabled);
            if (doTransition && transition != null)
            {
                OnTransitionBegin();
                transition.Enter(onFinishAll);
            }
            else
            {
                onFinishAll.Invoke();
            }
        }

        public virtual void Hide(bool doTransition, Action onFinish)
        {
            if (!active)
            {
                onFinish?.Invoke();
                return;
            }

            Action onFinishAll = () =>
            {
                OnHideFinish();
                onFinish?.Invoke();
            };
            var transition = transitions.FirstOrDefault(t => t.enabled);
            if (doTransition && transition != null)
            {
                OnTransitionBegin();
                transition.Exit(OnHideComplete, onFinishAll);
            }
            else
            {
                OnHideComplete();
                myPanel.SetActive(false);
                onFinishAll.Invoke();
            }
        }

        protected virtual void Start()
        {
            var parent = transform.parent.GetComponentInParent<PanelController>(true);
            if (parent != null)
            {
                // Let the parent init layout for this
                return;
            }

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
    }
}
