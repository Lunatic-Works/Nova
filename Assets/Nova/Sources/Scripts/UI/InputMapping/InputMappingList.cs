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
            var cnt = controller.currentCompoundKeys.Count;
            InputMappingListEntry entry = null;
            for (var i = 0; i < cnt; i++)
            {
                entry = Instantiate(entryPrefab, content);
                entry.Init(controller, i);
            }

            return entry;
        }
    }
}