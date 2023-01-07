using UnityEngine;

namespace Nova
{
    public class QualityManager : MonoBehaviour
    {
        private void Awake()
        {
            // On Windows/Linux/macOS/WebGL, this is overriden by vSyncCount
            // On Android/iOS, there is no VSync, so we need this
            Application.targetFrameRate = 60;
        }
    }
}
