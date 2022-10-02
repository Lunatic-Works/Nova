using UnityEngine;

namespace Nova
{
    public class BookmarkSaveBehaviour : MonoBehaviour
    {
        private GameState gameState;
        private SaveViewController saveViewController;

        private string currentNodeName;
        private string lastSavedNodeName;

        private void Start()
        {
            gameState = Utils.FindNovaGameController().GameState;
            saveViewController = Utils.FindViewManager().GetController<SaveViewController>();

            gameState.nodeChanged.AddListener(OnNodeChanged);
            gameState.selectionOccurs.AddListener(OnSelectionOccurs);
        }

        private void OnDestroy()
        {
            gameState.nodeChanged.RemoveListener(OnNodeChanged);
            gameState.selectionOccurs.RemoveListener(OnSelectionOccurs);
        }

        private void OnNodeChanged(NodeChangedData nodeChangedData)
        {
            currentNodeName = nodeChangedData.nodeHistoryEntry.Key;
        }

        private void OnSelectionOccurs(SelectionOccursData selectionOccursData)
        {
            if (currentNodeName == lastSavedNodeName)
            {
                return;
            }

            saveViewController.AutoSaveBookmark();
            lastSavedNodeName = currentNodeName;
        }
    }
}
