using UnityEngine;

namespace Nova
{
    public class ToggleFullScreen : MonoBehaviour
    {
        private InputSystemManager inputManager;

        private void Awake()
        {
            inputManager = Utils.FindNovaGameController().InputManager;
        }

        private void Update()
        {
            if (inputManager.IsTriggered(AbstractKey.ToggleFullScreen))
            {
                GameRenderManager.SwitchFullScreen();
            }
        }
    }
}