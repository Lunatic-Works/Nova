using UnityEngine;

namespace Nova
{
    public class ToggleFullscreen : MonoBehaviour
    {
        private InputMapper inputMapper;

        private void Awake()
        {
            inputMapper = Utils.FindNovaGameController().InputMapper;
        }

        private void Update()
        {
            if (inputMapper.GetKeyUp(AbstractKey.ToggleFullscreen))
            {
                GameRenderManager.SwitchFullScreen();
            }
        }
    }
}