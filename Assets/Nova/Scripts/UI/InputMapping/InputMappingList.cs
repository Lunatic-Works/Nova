using System.Linq;
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
            var children = content.Cast<Transform>().ToList();

            foreach (var child in children)
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

        public void Refresh()
        {
            ClearContent();
            var cnt = controller.currentCompoundKeys.Count;
            for (var i = 0; i < cnt; i++)
            {
                var go = Instantiate(entryPrefab, content);
                go.Init(controller, i);
            }
        }
    }
}