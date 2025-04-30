using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Base class for properties controlled by NovaAnimation.
    /// </summary>
    public class AnimationProperty : IDisposable
    {
        protected static Dictionary<string, int> locks = new Dictionary<string, int>();

        private readonly string key;

        private bool lockAcquired;

        protected AnimationProperty(string key)
        {
            this.key = key;
        }

        protected void AcquireLock()
        {
            if (lockAcquired)
            {
                return;
            }

            // Debug.Log($"AcquireLock {key.GetHashCode()} {key}");

            if (locks.ContainsKey(key))
            {
                if (locks[key] > 0)
                {
                    Debug.LogWarning($"Nova: AnimationProperty lock already acquired for {key}");
                }

                ++locks[key];
            }
            else
            {
                locks[key] = 1;
            }

            lockAcquired = true;
        }

        private void ReleaseLock()
        {
            // Debug.Log($"ReleaseLock {key.GetHashCode()} {key}");

            if (!lockAcquired)
            {
                Debug.LogWarning($"Nova: Release lock without lockAcquired for {key}");
                return;
            }

            if (!locks.ContainsKey(key))
            {
                Debug.LogWarning($"Nova: Release lock but key is not in locks for {key}");
                return;
            }

            if (locks[key] <= 0)
            {
                Debug.LogWarning($"Nova: Release lock but lock count {locks[key]} <= 0 for {key}");
                return;
            }

            --locks[key];
        }

        public virtual void Dispose()
        {
            ReleaseLock();
        }

        /// <summary>
        /// The parameter to interpolate between the start and the target values, usually ranging in [0, 1].
        /// </summary>
        public virtual float value { get; set; }
    }
}
