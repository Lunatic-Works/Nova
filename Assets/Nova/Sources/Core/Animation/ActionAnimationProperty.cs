using System;
using LuaInterface;
using UnityEngine;

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
            get => 0f;
            set
            {
                try
                {
                    action?.Invoke();
                }
                catch (LuaException e)
                {
                    // Do not let the exception halt the animation chain
                    Debug.LogException(e);
                }
            }
        }
    }
}
