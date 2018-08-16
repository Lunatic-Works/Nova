using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class NodeChangeListener : MonoBehaviour
    {
        private Text _text;

        private GameState _gameState;

        private void Awake()
        {
            _text = transform.Find("Text").GetComponent<Text>();
            _gameState = Utils.FindNovaGameController().GetComponent<GameState>();
            _gameState.NodeChanged += OnNodeChanged;
        }

        private void OnDestroy()
        {
            _gameState.NodeChanged -= OnNodeChanged;
        }

        private void OnNodeChanged(NodeChangedData nodeChangedData)
        {
            _text.text = nodeChangedData.nodeName;
        }
    }
}