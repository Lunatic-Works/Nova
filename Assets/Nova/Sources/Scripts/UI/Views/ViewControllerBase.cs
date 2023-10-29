using UnityEngine.InputSystem;

namespace Nova
{
    public abstract class ViewControllerBase : PanelController, IViewController
    {
        protected InputManager inputManager;

        protected override bool Init()
        {
            if (base.Init())
            {
                return true;
            }

            viewManager.SetController(this);
            inputManager = Utils.FindNovaController().InputManager;
            return false;
        }

        // Extension method does not work with Unity Action
        public void Show()
        {
            Show(true, null);
        }

        public void Hide()
        {
            Hide(true, null);
        }

        protected virtual void OnDestroy()
        {
            viewManager.UnsetController(this);
        }

        protected override void OnTransitionBegin()
        {
            viewManager.UpdateView(true);
        }

        protected override void OnShowFinish()
        {
            viewManager.UpdateView(false);
        }

        protected override void OnHideFinish()
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
