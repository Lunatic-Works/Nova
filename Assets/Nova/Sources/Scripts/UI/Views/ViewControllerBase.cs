using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nova
{
    public abstract class ViewControllerBase : PanelController, IViewController
    {
        public ViewManager viewManager { get; private set; }
        protected InputManager inputManager;

        protected override bool Init()
        {
            if (base.Init())
            {
                return true;
            }

            viewManager = GetComponentInParent<ViewManager>();
            this.RuntimeAssert(viewManager != null, "Missing ViewManager in parents.");
            viewManager.SetController(this);
            inputManager = Utils.FindNovaController().InputManager;
            return false;
        }

        protected virtual void OnDestroy()
        {
            viewManager.UnsetController(this);
        }

        protected override void onTransitionBegin()
        {
            viewManager.UpdateView(true);
        }

        protected override void onShowComplete()
        {
            viewManager.UpdateView(false);
        }

        protected override void OnHideComplete()
        {
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
            if (active && viewManager.currentView == CurrentViewType.UI)
            {
                OnActivatedUpdate();
            }
        }
    }
}
