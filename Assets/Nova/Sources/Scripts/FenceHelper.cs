using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    public class FenceHelper : MonoBehaviour
    {
        private GameState gameState;

        private void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;
            LuaRuntime.Instance.BindObject("fenceHelper", this);
        }

        public void SignalFence(object value)
        {
            gameState.SignalFence(value);
        }
    }
}