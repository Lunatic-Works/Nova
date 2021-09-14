using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class BranchController : MonoBehaviour, IRestorable
    {
        public BranchButtonController branchButtonPrefab;
        public GameObject backPanel;
        public string imageFolder;

        private GameState gameState;

        private void Awake()
        {
            gameState = Utils.FindNovaGameController().GameState;
            gameState.SelectionOccurs += RaiseSelectionsCallback;
            gameState.AddRestorable(this);
        }

        private void OnDestroy()
        {
            gameState.SelectionOccurs -= RaiseSelectionsCallback;
            gameState.RemoveRestorable(this);
        }

        private void RaiseSelectionsCallback(SelectionOccursData data)
        {
            RaiseSelections(data.selections);
        }

        public void RaiseSelections(IReadOnlyList<SelectionOccursData.Selection> selections)
        {
            if (selections.Count == 0)
            {
                return;
            }

            if (backPanel != null)
            {
                backPanel.SetActive(true);
            }

            for (var i = 0; i < selections.Count; i++)
            {
                var selection = selections[i];
                var index = i;
                var button = Instantiate(branchButtonPrefab, transform);
                button.Init(selection.texts, selection.imageInfo, imageFolder, () => Select(index), selection.active);
            }
        }

        private void Select(int index)
        {
            if (backPanel != null)
            {
                backPanel.SetActive(false);
            }

            RemoveAllSelectButton();
            gameState.SignalFence(index);
        }

        private void RemoveAllSelectButton()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        public string restorableObjectName => "BranchController";

        public IRestoreData GetRestoreData()
        {
            return null;
        }

        public void Restore(IRestoreData restoreData)
        {
            if (backPanel != null)
            {
                backPanel.SetActive(false);
            }

            RemoveAllSelectButton();
        }
    }
}