using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Assertions;
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
            Assert.IsNotNull(_sceneLoadController, "_sceneLoadController != null");
            Assert.IsNotNull(_gameState, "_gameState != null");
            _sceneLoadController.DoAfterLoad = _gameState.GameStart;
            SceneManager.LoadScene(GameSceneName);
        }
    }
}