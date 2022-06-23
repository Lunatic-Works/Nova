using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Nova
{
    public class RecordPopupController : ViewControllerBase
    {
        public RecordPopupLabel label;

        public List<InputControl> bindings
        {
            set => label.bindings = value;
        }

        protected override void OnActivatedUpdate()
        {
            // Do nothing
        }
    }
}