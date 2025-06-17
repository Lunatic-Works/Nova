using UnityEngine;

namespace Nova
{
    public class QualityManager : MonoBehaviour
    {
        private void Awake()
        {
            // On Windows/Linux/macOS, this is overriden by vSyncCount
            // On Android/iOS, there is no VSync, so we need this
            // On WebGL, this is not recommended, see https://discussions.unity.com/t/rendering-without-using-requestanimationframe-for-the-main-loop/608619
#if !UNITY_WEBGL
            Application.targetFrameRate = 60;
#endif
        }
    }
}
