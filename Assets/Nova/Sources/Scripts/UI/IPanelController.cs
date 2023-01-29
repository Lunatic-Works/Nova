using System;

namespace Nova
{
    public interface IPanelController
    {
        bool active { get; }

        void Hide(Action onFinish);

        void Show(Action onFinish);

        void ShowImmediate(Action onFinish);

        void HideImmediate(Action onFinish);
    }
}
