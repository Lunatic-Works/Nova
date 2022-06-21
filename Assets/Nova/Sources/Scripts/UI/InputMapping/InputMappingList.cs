using UnityEngine;

namespace Nova
{
    public class InputMappingList : MonoBehaviour
    {
        public Transform content;
        public InputMappingListEntry entryPrefab;
        public InputMappingController controller;

        private void ClearContent()
        {
            foreach (var child in Utils.GetChildren(content))
            {
                Destroy(child.gameObject);
            }
        }

        public void AddCompoundKey()
        {
            controller.AddCompoundKey();
        }

        public void RestoreCurrentKeyMapping()
        {
            controller.RestoreCurrentKeyMapping();
        }

        public void ResetCurrentKeyMappingDefault()
        {
            controller.ResetCurrentKeyMappingDefault();
        }

        public InputMappingListEntry Refresh()
        {
            ClearContent();
            InputMappingListEntry entry = null;
            foreach (var data in controller.bindingData)
            {
                var newEntry = Instantiate(entryPrefab, content);
                newEntry.Init(controller, data);
                if (entry == null || data.startIndex > entry.bindingData.startIndex)
                {
                    entry = newEntry;
                }
            }
            return entry;
        }
    }
}