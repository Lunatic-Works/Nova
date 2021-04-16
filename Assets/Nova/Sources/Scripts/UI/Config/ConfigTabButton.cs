using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Button))]
    public class ConfigTabButton : MonoBehaviour
    {
        public GameObject tabPanel;
        public Button button { get; private set; }

        private void Awake()
        {
            button = GetComponent<Button>();
        }
    }
}