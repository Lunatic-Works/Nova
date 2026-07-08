using System.Linq;
using Nova;
using NUnit.Framework;

namespace Tests
{
    public class TestDiffer
    {
        private static ulong[] Range(int begin, int count)
        {
            return Enumerable.Range(begin, count).Select(x => (ulong)x).ToArray();
        }

        private static void AssertMapInvariants(Differ differ, int oldLength, int newLength)
        {
            Assert.AreEqual(newLength, differ.remap.Count);
            Assert.AreEqual(oldLength, differ.leftMap.Count);
            Assert.AreEqual(oldLength, differ.rightMap.Count);
        }

        [Test]
        public void TestEqualListsMapByIndex()
        {
            var oldHashes = Range(0, 5);
            var newHashes = Range(0, 5);
            var differ = new Differ(oldHashes, newHashes);

            differ.GetDiffs();

            AssertMapInvariants(differ, oldHashes.Length, newHashes.Length);
            CollectionAssert.AreEqual(new[] {0, 1, 2, 3, 4}, differ.remap);
            CollectionAssert.AreEqual(new[] {0, 1, 2, 3, 4}, differ.leftMap);
            CollectionAssert.AreEqual(new[] {0, 1, 2, 3, 4}, differ.rightMap);
        }

        [Test]
        public void TestInsertMapsNewDialogueToMinusOne()
        {
            var oldHashes = new ulong[] {1, 2, 4};
            var newHashes = new ulong[] {1, 2, 3, 4};
            var differ = new Differ(oldHashes, newHashes);

            differ.GetDiffs();

            AssertMapInvariants(differ, oldHashes.Length, newHashes.Length);
            CollectionAssert.AreEqual(new[] {0, 1, -1, 2}, differ.remap);
            CollectionAssert.AreEqual(new[] {0, 1, 3}, differ.leftMap);
            CollectionAssert.AreEqual(new[] {0, 1, 3}, differ.rightMap);
        }

        [Test]
        public void TestDeleteMapsOldDialogueToNeighbor()
        {
            var oldHashes = new ulong[] {1, 2, 3, 4};
            var newHashes = new ulong[] {1, 2, 4};
            var differ = new Differ(oldHashes, newHashes);

            differ.GetDiffs();

            AssertMapInvariants(differ, oldHashes.Length, newHashes.Length);
            CollectionAssert.AreEqual(new[] {0, 1, 3}, differ.remap);
            CollectionAssert.AreEqual(new[] {0, 1, 1, 2}, differ.leftMap);
            CollectionAssert.AreEqual(new[] {0, 1, 2, 2}, differ.rightMap);
        }

        [Test]
        public void TestNaiveFallbackWithLongerNewListKeepsMapLengths()
        {
            var oldHashes = Range(0, 12);
            var newHashes = Range(100, 15);
            var differ = new Differ(oldHashes, newHashes);

            differ.GetDiffs();

            AssertMapInvariants(differ, oldHashes.Length, newHashes.Length);
            CollectionAssert.AreEqual(Enumerable.Range(0, 12), differ.remap.Take(12));
            CollectionAssert.AreEqual(Enumerable.Repeat(-1, 3), differ.remap.Skip(12));
            CollectionAssert.AreEqual(Enumerable.Range(0, 12), differ.leftMap);
            CollectionAssert.AreEqual(Enumerable.Range(0, 12), differ.rightMap);
        }

        [Test]
        public void TestNaiveFallbackWithLongerOldListKeepsMapLengths()
        {
            var oldHashes = Range(0, 15);
            var newHashes = Range(100, 12);
            var differ = new Differ(oldHashes, newHashes);

            differ.GetDiffs();

            AssertMapInvariants(differ, oldHashes.Length, newHashes.Length);
            CollectionAssert.AreEqual(Enumerable.Range(0, 12), differ.remap);
            CollectionAssert.AreEqual(Enumerable.Range(0, 12).Concat(Enumerable.Repeat(11, 3)), differ.leftMap);
            CollectionAssert.AreEqual(Enumerable.Range(0, 12).Concat(Enumerable.Repeat(12, 3)), differ.rightMap);
        }
    }
}
