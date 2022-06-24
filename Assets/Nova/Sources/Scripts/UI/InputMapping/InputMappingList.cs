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

        public void AddBinding()
        {
            controller.AddBinding();
        }

        public void RestoreCurrentKeyMapping()
        {
            controller.RestoreCurrentKeyMapping();
        }

        public void ResetCurrentKeyMappingDefault()
        {
            controller.ResetCurrentKeyMappingDefault();
        }

        public void Refresh()
        {
            ClearContent();
            foreach (var data in controller.compositeBindings)
            {
                var entry = Instantiate(entryPrefab, content);
                entry.Init(controller, data);
            }
        }
    }
}