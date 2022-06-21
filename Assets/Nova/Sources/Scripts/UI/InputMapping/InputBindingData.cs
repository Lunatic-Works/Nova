using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace Nova
{
    public class InputBindingData
    {
        public InputAction action;
        /// <summary>
        /// Inclusive start index of the binding
        /// </summary>
        public int startIndex { get; private set; }

        /// <summary>
        /// Exclusive end index of the binding.
        /// If endIndex = startIndex, the binding does not exist.
        /// </summary>
        public int endIndex { get; private set; }

        public IEnumerable<InputBinding> bindings
        {
            get
            {
                RefreshEndIndex();
                if (startIndex >= endIndex)
                {
                    yield break;
                }
                if (startIndex == endIndex - 1)
                {
                    yield return action.bindings[startIndex];
                }
                for (var i = startIndex + 1; i < endIndex; i++)
                {
                    yield return action.bindings[i];
                }
            }
        }

        public InputBinding? button => startIndex >= endIndex ? null
            : (InputBinding?)action.bindings[endIndex - 1];

        public string displayString => string.Join(" + ", bindings.Select(b => b.ToDisplayString()));

        public int RefreshEndIndex()
        {
            if (action.bindings.Count <= startIndex)
            {
                endIndex = startIndex;
                return endIndex;
            }
            endIndex = startIndex + 1;
            if (action.bindings[startIndex].isComposite)
            {
                while (endIndex < action.bindings.Count && action.bindings[endIndex].isPartOfComposite)
                {
                    ++endIndex;
                }
            }
            return endIndex;
        }

        /// <summary>
        /// Check if two binding data have the same button, ignoring modifiers.
        /// </summary>
        public bool SameButtonAs(InputBindingData y)
        {
            return button?.effectivePath == y.button?.effectivePath;
        }

        public InputBindingData(InputAction action, int startIndex)
        {
            this.action = action;
            this.startIndex = startIndex;
            RefreshEndIndex();
        }
    }

}