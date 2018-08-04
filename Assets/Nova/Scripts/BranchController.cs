using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class BranchController : MonoBehaviour, IRestorable
    {
        public GameState gameState;
        public GameObject BranchButtonPrefab;
        public GameObject blackPanel;

        private void Start()
        {
            gameState.BranchOccurs.AddListener(OnBranchHappen);
            gameState.AddRestorable(this);
        }

        /// <summary>
        /// Show branch buttons when branch happens
        /// </summary>
        /// <param name="branchOccursEventData"></param>
        private void OnBranchHappen(BranchOccursEventData branchOccursEventData)
        {
            blackPanel.SetActive(true);

            var branchInformations = branchOccursEventData.branchInformations;
            foreach (var branchInformation in branchInformations)
            {
                var childButton = Instantiate(BranchButtonPrefab);
                childButton.transform.SetParent(transform);
                var text = childButton.GetComponent<Text>();
                if (text == null)
                {
                    text = childButton.GetComponentInChildren<Text>();
                }
                text.text = branchInformation.name;

                childButton.GetComponent<Button>().onClick.AddListener(() => Select(branchInformation.name));
            }
        }

        /// <summary>
        /// Select a branch with the given name
        /// </summary>
        /// <param name="branchName">the name of the branch to select</param>
        private void Select(string branchName)
        {
            blackPanel.SetActive(false);

            gameState.SelectBranch(branchName);
            RemoveAllSelectButton();
        }

        private void RemoveAllSelectButton()
        {
            foreach (Transform child in transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        public string restorableName;

        public string restorableObjectName
        {
            get { return restorableName; }
        }

        public IRestoreData GetRestoreData()
        {
            return null;
        }

        public void Restore(IRestoreData restoreData)
        {
            blackPanel.SetActive(false);

            RemoveAllSelectButton();
        }
    }
}