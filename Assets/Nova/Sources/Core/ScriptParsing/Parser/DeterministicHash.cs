using System.Collections.Generic;

namespace Nova.Parser
{
    // The default GetHashCode() is randomized and will change every time when the program starts
    public interface IDeterministicHashable
    {
        ulong GetDeterministicHash();
    }

    public static class DeterministicHash
    {
        // Knuth's golden ratio multiplicative hashing
        public static ulong Add(ulong x, ulong y)
        {
            unchecked
            {
                return x * 11400714819323199563UL + y;
            }
        }

        public static ulong HashString(string s)
        {
            if (s == null)
            {
                return 0UL;
            }

            var r = 0UL;
            foreach (var x in s)
            {
                r = Add(r, x);
            }

            return r;
        }

        public static ulong HashList(IEnumerable<IDeterministicHashable> xs)
        {
            var r = 0UL;
            foreach (var x in xs)
            {
                r = Add(r, x.GetDeterministicHash());
            }

            return r;
        }
    }
}
