using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class PanelController : MonoBehaviour, IPanelController
    {
        public GameObject myPanel;

        protected List<UIViewTransitionBase> transitions;
        protected bool inited;

        protected virtual bool Init()
        {
            if (inited)
            {
                return true;
            }

            this.RuntimeAssert(myPanel != null, "Missing myPanel.");
            transitions = myPanel.GetComponents<UIViewTransitionBase>().ToList();

            inited = true;
            return false;
        }

        protected virtual void Awake()
        {
            Init();
        }

        public bool active => myPanel.activeSelf;

        protected virtual void onTransitionBegin() { }

        protected virtual void onShowComplete() { }

        protected virtual void OnHideComplete() { }

        public virtual void Show(Action onFinish)
        {
            if (active)
            {
                onFinish?.Invoke();
                return;
            }

            myPanel.SetActive(true);
            var transition = transitions.FirstOrDefault(t => t.enabled);
            if (transition != null)
            {
                onTransitionBegin();
                transition.Enter(() =>
                {
                    onShowComplete();
                    onFinish?.Invoke();
                });
            }
            else
            {
                onShowComplete();
                onFinish?.Invoke();
            }
        }

        public virtual void Hide(Action onFinish)
        {
            if (!active)
            {
                onFinish?.Invoke();
                return;
            }

            var transition = transitions.FirstOrDefault(t => t.enabled);
            if (transition != null)
            {
                onTransitionBegin();
                transition.Exit(() =>
                {
                    OnHideComplete();
                    myPanel.SetActive(false);
                    onFinish?.Invoke();
                });
            }
            else
            {
                OnHideComplete();
                myPanel.SetActive(false);
                onFinish?.Invoke();
            }
        }

        public void ShowImmediate(Action onFinish)
        {
            myPanel.SetActive(true);
            onShowComplete();
            onFinish?.Invoke();
        }

        public void HideImmediate(Action onFinish)
        {
            OnHideComplete();
            myPanel.SetActive(false);
            onFinish?.Invoke();
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
    }
}
