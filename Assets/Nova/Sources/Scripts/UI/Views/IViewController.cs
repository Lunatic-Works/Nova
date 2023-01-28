using System;

namespace Nova
{
    public interface IViewController
    {
        ViewManager viewManager { get; }
        bool active { get; }

        void Hide(Action onFinish);

        void Hide();

        void Show(Action onFinish);

        void Show();
    }
}
