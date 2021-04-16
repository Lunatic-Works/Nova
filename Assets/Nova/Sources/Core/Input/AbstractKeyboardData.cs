using System.Collections.Generic;
using System.Linq;

namespace Nova
{
    public class AbstractKeyboardData : Dictionary<AbstractKey, List<CompoundKey>>
    {
        public AbstractKeyboardData GetCopy()
        {
            var copy = new AbstractKeyboardData();
            foreach (var pair in this)
            {
                copy[pair.Key] = pair.Value.Select(key => new CompoundKey(key)).ToList();
            }

            return copy;
        }
    }
}