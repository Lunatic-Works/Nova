using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Nova.Examples.Colorless.Scripts
{
    public class EndImageController : MonoBehaviour, IPointerClickHandler
    {
        private GameState _gameState;
        public string titleSceneName;

        private void Awake()
        {
            _gameState = Utils.FindNovaGameController().GetComponent<GameState>();
            _gameState.CurrentRouteEnded += OnCurrentRouteEnded;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _gameState.CurrentRouteEnded -= OnCurrentRouteEnded;
        }

        private void OnCurrentRouteEnded(CurrentRouteEndedData arg0)
        {
            Debug.Log("Ended");
            gameObject.SetActive(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SceneManager.LoadScene(titleSceneName);
        }
    }
}