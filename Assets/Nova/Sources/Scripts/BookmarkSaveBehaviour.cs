using UnityEngine;

namespace Nova
{
    public class BookmarkSaveBehaviour : MonoBehaviour
    {
        private GameState gameState;
        private SaveViewController saveViewController;

        private void Start()
        {
            gameState = Utils.FindNovaGameController().GameState;
            saveViewController = GetComponent<SaveViewController>();

            gameState.selectionOccurs.AddListener(OnSelectionOccurs);
        }

        private void OnDestroy()
        {
            gameState.selectionOccurs.RemoveListener(OnSelectionOccurs);
        }

        private void OnSelectionOccurs(SelectionOccursData selectionOccursData)
        {
            saveViewController.AutoSaveBookmark();
        }
    }
}