using UnityEngine;

namespace Nova
{
    public class RecordPopupController : ViewControllerBase
    {
        [SerializeField] private RecordPopupLabel label;

        public InputMappingEntry entry
        {
            set => label.entry = value;
        }

        protected override void OnActivatedUpdate()
        {
            // Do nothing
        }
    }
}
