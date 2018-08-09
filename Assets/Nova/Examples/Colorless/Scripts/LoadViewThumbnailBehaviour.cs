using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nova.Examples.Colorless.Scripts
{
    [RequireComponent(typeof(SaveViewController))]
    public class LoadViewThumbnailBehaviour : MonoBehaviour
    {
        private SceneLoadController _sceneLoadController;
        private GameState _gameState;
        private SaveViewController _saveViewController;

        public string NextSceneName;

        private void Awake()
        {
            var novaGameController = Utils.FindNovaGameController();
            _sceneLoadController = novaGameController.GetComponent<SceneLoadController>();
            _gameState = novaGameController.GetComponent<GameState>();
            _saveViewController = GetComponent<SaveViewController>();
            _saveViewController.BookmarkLoad.AddListener(OnLoad);
        }

        private void OnLoad(BookmarkLoadEventData data)
        {
            _sceneLoadController.DoAfterLoad = () => _gameState.LoadBookmark(data.bookmark);
            SceneManager.LoadScene(NextSceneName);
        }
    }
}