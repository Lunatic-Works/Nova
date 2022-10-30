using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Popup dialog on window close
    /// </summary>
    public class CloseWindowHandler : MonoBehaviour
    {
        private static bool WantsToQuit()
        {
            if (Utils.ForceQuit)
            {
                return true;
            }

            Utils.QuitWithAlert();
            return Utils.ForceQuit;
        }

        private void Start()
        {
            Application.wantsToQuit += WantsToQuit;
        }
    }
}
