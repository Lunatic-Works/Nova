using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class BranchController : MonoBehaviour, IRestorable
    {
        public GameObject BranchButtonPrefab;
        public GameObject blackPanel;
        public bool dimOnBranch;

        private GameState gameState;

        private void Awake()
        {
            gameState = Utils.FindNovaGameController().GetComponent<GameState>();
            gameState.BranchOccurs += OnBranchHappen;
            gameState.AddRestorable(this);
        }

        private void OnDestroy()
        {
            gameState.BranchOccurs -= OnBranchHappen;
            gameState.RemoveRestorable(this);
        }

        /// <summary>
        /// Show branch buttons when branch happens
        /// </summary>
        /// <param name="branchOccursData"></param>
        private void OnBranchHappen(BranchOccursData branchOccursData)
        {
            if (dimOnBranch)
            {
                blackPanel.SetActive(true);
            }

            var branchInformations = branchOccursData.branchInformations;
            foreach (var branchInformation in branchInformations)
            {
                var childButton = Instantiate(BranchButtonPrefab);
                childButton.transform.SetParent(transform);
                childButton.transform.localScale = Vector3.one;

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