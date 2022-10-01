using UnityEngine;

namespace Nova
{
    public class AutoSave : MonoBehaviour
    {
        public float maxTime = 10.0f;

        private bool isPaused;
        private float time;

        private void Update()
        {
            if (isPaused || time >= maxTime)
            {
                return;
            }

            time += Time.deltaTime;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            isPaused = !hasFocus;
            TrySave();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            isPaused = pauseStatus;
            TrySave();
        }

        private void TrySave()
        {
            if (!isPaused || time < maxTime)
            {
                return;
            }

            Utils.SaveAll();
            time = 0.0f;
        }
    }
}
