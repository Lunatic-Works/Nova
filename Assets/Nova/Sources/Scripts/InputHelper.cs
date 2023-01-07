using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class InputHelper : MonoBehaviour
    {
        private NovaController novaController;

        private void Awake()
        {
            novaController = Utils.FindNovaController();
            LuaRuntime.Instance.BindObject("inputHelper", this);
        }

        public void DisableInput()
        {
            novaController.DisableInput();
        }

        public void EnableInput()
        {
            novaController.EnableInput();
        }
    }
}
