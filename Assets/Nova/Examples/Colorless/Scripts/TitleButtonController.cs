using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nova.Examples.Colorless.Scripts
{
    public class TitleButtonController : MonoBehaviour
    {
        private AlertController _alert;
        public string AlertMessage;

        public string titleSceneName;

        private void Awake()
        {
            _alert = GameObject.FindWithTag("Alert").GetComponent<AlertController>();
            var button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            _alert.Alert(null, AlertMessage, () => SceneManager.LoadScene(titleSceneName));
        }
    }
}