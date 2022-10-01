using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace Nova
{
    public class CompositeInputBinding
    {
        private readonly InputAction action;

        /// <summary>
        /// Inclusive start index of the binding.
        /// </summary>
        /// <remarks>
        /// startIndex and endIndex will be out of sync if action.bindings is mutated.
        /// In that case, you should recreate all CompositeInputBinding,
        /// e.g., call InputMappingController.RefreshCompositeBindings.
        /// </remarks>
        public readonly int startIndex;

        /// <summary>
        /// Exclusive end index of the binding.
        /// If endIndex == startIndex, the binding does not exist.
        /// </summary>
        public readonly int endIndex;

        private IEnumerable<InputBinding> bindings
        {
            get
            {
                if (startIndex >= endIndex)
                {
                    yield break;
                }

                if (startIndex == endIndex - 1)
                {
                    yield return action.bindings[startIndex];
                }

                // A composite binding takes an index at the beginning
                for (var i = startIndex + 1; i < endIndex; ++i)
                {
                    yield return action.bindings[i];
                }
            }
        }

        public override string ToString()
        {
            return string.Join(" + ", bindings.Select(b => b.ToDisplayString()));
        }

        public bool AnySameBinding(CompositeInputBinding other)
        {
            return bindings.Any(x => other.bindings.Any(y => x.effectivePath == y.effectivePath));
        }

        public CompositeInputBinding(InputAction action, int startIndex)
        {
            this.action = action;
            this.startIndex = startIndex;

            if (action.bindings.Count <= startIndex)
            {
                endIndex = startIndex;
            }
            else
            {
                endIndex = startIndex + 1;
                if (action.bindings[startIndex].isComposite)
                {
                    while (endIndex < action.bindings.Count && action.bindings[endIndex].isPartOfComposite)
                    {
                        ++endIndex;
                    }
                }
            }
        }
    }
}
