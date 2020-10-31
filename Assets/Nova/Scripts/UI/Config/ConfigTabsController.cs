using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class ConfigTabsController : MonoBehaviour
    {
        public List<ConfigTabButton> tabs;

        private void Start()
        {
            this.RuntimeAssert(tabs.Count > 0, "Empty Config Tab List");
            for (var i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                if (tab.isActiveAndEnabled)
                {
                    var index = i;
                    tab.button.onClick.AddListener(() => SetActiveTab(index));
                }
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