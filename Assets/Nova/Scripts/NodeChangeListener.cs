using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class NodeChangeListener : MonoBehaviour
    {
        private Text _text;

        private void Start()
        {
            _text = transform.Find("Text").GetComponent<Text>();
        }

        public void OnNodeChanged(NodeChangedEventData nodeChangedEventData)
        {
            _text.text = nodeChangedEventData.nodeName;
        }
    }
}