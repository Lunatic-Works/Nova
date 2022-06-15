using System;
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
            gameState.selectionOccurs.AddListener(RaiseSelectionsCallback);
            gameState.AddRestorable(this);
        }

        private void OnDestroy()
        {
            gameState.selectionOccurs.RemoveListener(RaiseSelectionsCallback);
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
                throw new ArgumentException("Nova: No active branch for selection.");
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
                // Prevent showing the button before init
                button.gameObject.SetActive(false);
                button.Init(selection.texts, selection.imageInfo, imageFolder, () => Select(index), selection.active);
                button.gameObject.SetActive(true);
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

        #region Restoration

        public string restorableName => "BranchController";

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

        #endregion
    }
}