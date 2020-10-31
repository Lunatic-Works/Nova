using UnityEngine.Assertions;

namespace Nova
{
    /// <summary>
    /// A trivial counting lock
    /// </summary>
    /// <remarks>
    /// The lock is considered locked only if the number of occupation exceeds the threshold
    /// </remarks>
    public class CounterLock
    {
        private int count;
        private readonly int threshold;

        public CounterLock() { }

        public CounterLock(int threshold)
        {
            this.threshold = threshold;
        }

        /// <summary>
        /// Check if the lock has been acquired
        /// </summary>
        public bool isLocked => count > threshold;

        /// <summary>
        /// Acquire the lock
        /// </summary>
        public void Acquire()
        {
            Assert.IsTrue(count < int.MaxValue, "Nova: More than Int32.MaxValue calls acquiring lock.");
            count++;
        }

        /// <summary>
        /// Release the lock
        /// </summary>
        public void Release()
        {
            Assert.IsTrue(count > 0, "Nova: Too many calls releasing lock.");
            count--;
        }
    }
}