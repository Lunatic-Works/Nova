using UnityEngine;

namespace Nova.Examples.Colorless.Scripts
{
    public class GameSceneStartBehaviour : MonoBehaviour
    {
        private SceneLoadController _sceneLoadController;

        private void Awake()
        {
            _sceneLoadController = Utils.FindNovaGameController().GetComponent<SceneLoadController>();
        }

        private void Start()
        {
            if (_sceneLoadController.DoAfterLoad != null)
            {
                _sceneLoadController.DoAfterLoad.Invoke();
            }
        }
    }
}