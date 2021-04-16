using UnityEngine;

namespace Nova
{
    public class ToggleFullScreen : MonoBehaviour
    {
        private InputMapper inputMapper;

        private void Awake()
        {
            inputMapper = Utils.FindNovaGameController().InputMapper;
        }

        private void Update()
        {
            if (inputMapper.GetKeyUp(AbstractKey.ToggleFullScreen))
            {
                GameRenderManager.SwitchFullScreen();
            }
        }
    }
}