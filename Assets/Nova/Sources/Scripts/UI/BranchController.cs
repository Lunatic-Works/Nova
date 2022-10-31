using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class BranchController : MonoBehaviour, IRestorable
    {
        [SerializeField] private BranchButtonController branchButtonPrefab;
        [SerializeField] private GameObject backPanel;
        [SerializeField] private string imageFolder;

        private GameState gameState;
        [HideInInspector] public int activeSelectionCount;

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
                throw new ArgumentException("Nova: No active selection.");
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
                button.Init(selection.texts, selection.imageInfo, imageFolder, () => Select(index),
                    selection.interactable);
                button.gameObject.SetActive(true);
            }

            activeSelectionCount = selections.Count;
        }

        public void Select(int index)
        {
            if (backPanel != null)
            {
                backPanel.SetActive(false);
            }

            RemoveAllSelections();
            gameState.SignalFence(index);
        }

        private void RemoveAllSelections()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            activeSelectionCount = 0;
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

            RemoveAllSelections();
        }

        #endregion
    }
}
