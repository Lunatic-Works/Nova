using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class InputHelper : MonoBehaviour
    {
        private InputManager inputManager;

        private void Awake()
        {
            inputManager = Utils.FindNovaController().InputManager;
            LuaRuntime.Instance.BindObject("inputHelper", this);
        }

        public bool inputEnabled
        {
            get => inputManager.inputEnabled;
            set => inputManager.inputEnabled = value;
        }
    }
}
