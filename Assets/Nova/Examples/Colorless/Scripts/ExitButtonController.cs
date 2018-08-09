using UnityEngine;

namespace Nova.Examples.Colorless.Scripts
{
    public class ExitButtonController: MonoBehaviour
    {
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}