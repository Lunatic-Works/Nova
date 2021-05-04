using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class InputHelper : MonoBehaviour
    {
        private GameController gameController;

        private void Awake()
        {
            gameController = Utils.FindNovaGameController();
            LuaRuntime.Instance.BindObject("inputHelper", this);
        }

        public void DisableInput()
        {
            gameController.DisableInput();
        }

        public void EnableInput()
        {
            gameController.EnableInput();
        }
    }
}