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
                // TODO: Auto save when UI showing
                if (Utils.FindViewManager().currentView == CurrentViewType.Game)
                {
                    AutoSaveBookmark.Current.TrySave();
                }

                return true;
            }

            Utils.QuitWithAlert();
            return Utils.ForceQuit;
        }

        // Do not trigger this in Awake
        private void Start()
        {
            Application.wantsToQuit += WantsToQuit;
        }
    }
}
