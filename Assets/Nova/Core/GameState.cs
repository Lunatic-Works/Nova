using UnityEngine;

namespace Nova
{
    /// <inheritdoc />
    /// <summary>
    /// This class manages the AVG game state.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public string scriptPath;

        private readonly ScriptLoader scriptLoader = new ScriptLoader();
        private readonly AssetsLoader assetsLoader = new AssetsLoader();
        private FlowChartTree flowChartTree;

        private void Awake()
        {
            LuaRuntime.Instance.Init();
            scriptLoader.Init(scriptPath);
            flowChartTree = scriptLoader.GetFlowChartTree();
        }

        private void OnApplicationQuit()
        {
            LuaRuntime.Instance.Dispose();
        }
    }
}