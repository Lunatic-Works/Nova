using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Text))]
    public class RecordPopupLabel : MonoBehaviour
    {
        public InputMappingListEntry entry;

        private Text label;

        private void Awake()
        {
            label = GetComponent<Text>();
        }

        private void Update()
        {
            label.text = entry.key.ToString();
        }
    }
}