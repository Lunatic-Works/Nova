using System;
using System.Collections;
using System.Collections.Generic;
using Nova;
using UnityEngine;
using UnityEngine.UI;

public class BranchController : MonoBehaviour
{
    public Button branchButtomPrefab;

    public GameState gameState;

    public void OnBranchHappen(BranchOccursEventData branchOccursEventData)
    {
        var branchInformations = branchOccursEventData.branchInformations;
        foreach (var branchInformation in branchInformations)
        {
            var childButtom = Instantiate(branchButtomPrefab);
            childButtom.transform.SetParent(transform);
            var text = childButtom.GetComponent<Text>();
            if (text == null)
            {
                text = childButtom.GetComponentInChildren<Text>();
            }

            text.text = branchInformation.name;
            childButtom.onClick.AddListener(() => Select(branchInformation.name));
        }
    }

    private void Select(string branchName)
    {
        gameState.SelectBranch(branchName);
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}