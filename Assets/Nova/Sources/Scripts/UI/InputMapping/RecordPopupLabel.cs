using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Text))]
    public class RecordPopupLabel : MonoBehaviour
    {
        public List<InputControl> controls;

        private Text label;

        private void Awake()
        {
            label = GetComponent<Text>();
        }

        private void Update()
        {
            label.text = string.Join(" + ", controls.Select(b => b.shortDisplayName ?? b.displayName));
        }
    }
}
