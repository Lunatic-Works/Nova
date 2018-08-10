using System;
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
        private int _count;
        private readonly int _threshold;

        public CounterLock()
        {
        }

        public CounterLock(int threshold)
        {
            _threshold = threshold;
        }

        /// <summary>
        /// Check if the lock has been aquired
        /// </summary>
        public bool isLocked
        {
            get { return _count > _threshold; }
        }

        /// <summary>
        /// Aquire the lock
        /// </summary>
        public void Aquire()
        {
            Assert.IsTrue(_count < int.MaxValue, "More than Int32.MaxValue calls aquiring lock");
            _count++;
        }

        /// <summary>
        /// Release the lock
        /// </summary>
        public void Release()
        {
            Assert.IsTrue(_count > 0, "Too many release lock");
            _count--;
        }
    }
}