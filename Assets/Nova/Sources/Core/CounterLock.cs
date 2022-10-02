using System.Collections;

namespace Nova
{
    /// <summary>
    /// A trivial counter lock (semaphore)
    /// </summary>
    /// <remarks>
    /// The lock is considered locked only if the number of acquisition exceeds the threshold
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
            Utils.RuntimeAssert(count < int.MaxValue, "More than Int32.MaxValue calls acquiring the lock.");
            count++;
        }

        /// <summary>
        /// Release the lock
        /// </summary>
        public void Release()
        {
            Utils.RuntimeAssert(count > 0, "Too many calls releasing the lock.");
            count--;
        }

        public void Reset()
        {
            count = threshold;
        }

        public IEnumerator WaitCoroutine()
        {
            while (isLocked)
            {
                yield return null;
            }
        }
    }
}
