using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Create and init NovaGameController prefab
    /// </summary>
    public class NovaCreator : MonoBehaviour
    {
        public GameObject novaGameControllerPrefab;

        private void Awake()
        {
            var controllerCount = GameObject.FindGameObjectsWithTag("NovaGameController").Length;
            if (controllerCount > 1)
            {
                Debug.LogWarning("Nova: Multiple NovaGameController found in the scene.");
            }

            if (controllerCount >= 1)
            {
                return;
            }

            var controller = Instantiate(novaGameControllerPrefab);
            controller.tag = "NovaGameController";
            DontDestroyOnLoad(controller);
        }
    }
}