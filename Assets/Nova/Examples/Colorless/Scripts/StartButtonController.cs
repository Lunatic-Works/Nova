using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;

namespace Nova.Examples.Colorless.Scripts
{
    public class StartButtonController : MonoBehaviour
    {
        public string GameSceneName;

        private SceneLoadController _sceneLoadController;
        private GameState _gameState;

        private void Awake()
        {
            var novaGameController = Utils.FindNovaGameController();
            _sceneLoadController = novaGameController.GetComponent<SceneLoadController>();
            _gameState = novaGameController.GetComponent<GameState>();
        }

        public void OnClick()
        {
            _sceneLoadController.DoAfterLoad = _gameState.GameStart;
            SceneManager.LoadScene(GameSceneName);
        }
    }
}