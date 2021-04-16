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
            LuaRuntime.Instance.BindObject("InputHelper", this);
        }

        public void DisableInput(bool value)
        {
            gameController.disableInput = value;
        }
    }
}