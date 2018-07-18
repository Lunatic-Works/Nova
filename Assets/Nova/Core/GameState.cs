using Boo.Lang;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    [System.Serializable]
    public class OnDialogueChangeEvent : UnityEvent<string>
    {
    }

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

        #region status

        /// <summary>
        /// Nodes that has been walked through
        /// </summary>
        private List<string> walkedThroughNodes;

        /// <summary>
        /// The index of the current dialogue entry in the current node
        /// </summary>
        private int currentIndex;

        /// <summary>
        /// The current flow chart node
        /// </summary>
        private FlowChartNode currentNode;

        /// <summary>
        /// The current dialogueEntry
        /// </summary>
        private DialogueEntry currentDialogueEntry;

        #endregion

        public OnDialogueChangeEvent OnDialogueChanged;

        /// <summary>
        /// Called after the current node or the index of the current dialogue entry has changed.
        /// </summary>
        /// <remarks>
        /// The game state will be updated according to the current node and current dialogue index.
        /// This method will execute the action in the new current dialogue entry and informs all game state listeners
        /// Since the action inside the dialogue entry will be executed, this method should not be called twice
        /// if only one update has happen
        /// </remarks>
        private void UpdateGameState()
        {
            currentDialogueEntry = currentNode.GetDialogueEntryAt(currentIndex);
            currentDialogueEntry.ExecuteAction();
            OnDialogueChanged.Invoke(currentDialogueEntry.text);
        }

        /// <summary>
        /// Move on to the next node
        /// </summary>
        /// <param name="nextNode">The next node to move to</param>
        private void MoveToNode(FlowChartNode nextNode)
        {
            walkedThroughNodes.Add(nextNode.name);
            currentNode = nextNode;
            currentIndex = 0;
            UpdateGameState();
        }

        /// <summary>
        /// Start the game from the given node
        /// </summary>
        /// <param name="startNode">The node from where the game starts</param>
        private void GameStart(FlowChartNode startNode)
        {
            // clear possible history
            walkedThroughNodes = new List<string>();
            MoveToNode(startNode);
        }

        /// <summary>
        /// Start the game from the default start up point
        /// </summary>
        public void GameStart()
        {
            var startNode = flowChartTree.DefaultStartUpNode;
            GameStart(startNode);
        }

        /// <summary>
        /// Start the game from a named start point 
        /// </summary>
        /// <param name="startName">the name of the start</param>
        public void Gamestart(string startName)
        {
            var startNode = flowChartTree.GetStartUpNode(startName);
            GameStart(startNode);
        }

        private void OnApplicationQuit()
        {
            LuaRuntime.Instance.Dispose();
        }
    }
}