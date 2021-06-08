using System;

namespace Nova
{
    /// <summary>
    /// The duration of action property should always be 0.
    /// The action will be invoked for the specified repeat times, once per frame.
    /// </summary>
    [ExportCustomType]
    public class ActionAnimationProperty : IAnimationProperty
    {
        private readonly Action action;

        public ActionAnimationProperty(Action action)
        {
            this.action = action;
        }

        public float value
        {
            get => 0.0f;
            set => action?.Invoke();
        }
    }
}