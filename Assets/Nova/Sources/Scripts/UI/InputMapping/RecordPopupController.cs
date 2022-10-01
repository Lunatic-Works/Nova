using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nova
{
    public class RecordPopupController : ViewControllerBase
    {
        [SerializeField] private RecordPopupLabel label;

        public List<InputControl> controls
        {
            set => label.controls = value;
        }

        protected override void OnActivatedUpdate()
        {
            // Do nothing
        }
    }
}
