using System;

namespace Nova
{
    public interface IPanelController
    {
        bool active { get; }

        void Hide(bool doTransition, Action onFinish);

        void Show(bool doTransition, Action onFinish);
    }
}
