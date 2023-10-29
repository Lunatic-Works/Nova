using UnityEngine;

namespace Nova
{
    // Flush data in memory to disk at intervals
    public class AutoFlush : MonoBehaviour
    {
        [SerializeField] private float maxTime = 10.0f;

        private bool isPaused;
        private float time;

        private void Update()
        {
            if (isPaused || time >= maxTime)
            {
                return;
            }

            time += Time.unscaledDeltaTime;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            isPaused = !hasFocus;
            TryFlush();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            isPaused = pauseStatus;
            TryFlush();
        }

        private void TryFlush()
        {
            if (!isPaused || time < maxTime)
            {
                return;
            }

            Utils.FlushAll();
            time = 0.0f;
        }
    }
}
