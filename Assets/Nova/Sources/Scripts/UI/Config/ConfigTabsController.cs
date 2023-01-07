using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class ConfigTabsController : MonoBehaviour
    {
        [SerializeField] private List<ConfigTabButton> tabs;

        private void Awake()
        {
            this.RuntimeAssert(tabs.Count > 0, "Empty config tab list.");
            for (var i = 0; i < tabs.Count; i++)
            {
                var index = i;
                tabs[i].GetComponent<Button>().onClick.AddListener(() => SetActiveTab(index));
            }

            SetActiveTab(0);
        }

        private void SetActiveTab(int index)
        {
            for (var i = 0; i < tabs.Count; i++)
            {
                tabs[i].tabPanel.SetActive(i == index);
            }
        }
    }
}
