using Boo.Lang;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    [System.Serializable]
    public class DialogueChangedEvent : UnityEvent<string>
    {
    }

    [System.Serializable]
    public class NodeChangedEvent : UnityEvent<string, string>
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

        /// <summary>
        /// This event will be triggered if the content of the dialogue has changed. New dialogue text will be
        /// sent to all listeners
        /// </summary>
        /// <remarks>
        /// The first parameter is the new dialogue text
        /// </remarks>
        public DialogueChangedEvent DialogueChanged;

        /// <summary>
        /// This event will be triggered if the node has changed. The name and discription of the new node will be
        /// sent to all listeners
        /// </summary>
        /// <remarks>
        /// The first parameter give the new node name, the second parameter give the new node description
        /// </remarks>
        public NodeChangedEvent NodeChanged;

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
            DialogueChanged.Invoke(currentDialogueEntry.text);
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
            NodeChanged.Invoke(currentNode.name, currentNode.description);
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
    }
}