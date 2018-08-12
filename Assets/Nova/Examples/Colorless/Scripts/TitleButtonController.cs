using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nova.Examples.Colorless.Scripts
{
    public class TitleButtonController : MonoBehaviour
    {
        private AlertController _alert;

        public string titleSceneName;

        private GameState _gameState;

        private void Awake()
        {
            _alert = GameObject.FindWithTag("Alert").GetComponent<AlertController>();
            var button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
            _gameState = Utils.FindNovaGameController().GetComponent<GameState>();
        }

        private void OnClick()
        {
            _alert.Alert(null, I18n.__("ingame.title.confirm"), () =>
            {
                _gameState.ResetGameState();
                SceneManager.LoadScene(titleSceneName);
            });
        }
    }
}