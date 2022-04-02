using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class DebugSelection : MonoBehaviour
    {
        private GameObject lastSelected;

        private void Update()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected == lastSelected)
            {
                return;
            }

            Debug.Log("selected: " + (selected ? Utils.GetPath(selected.transform) : "null"));

            lastSelected = selected;
        }
    }
}