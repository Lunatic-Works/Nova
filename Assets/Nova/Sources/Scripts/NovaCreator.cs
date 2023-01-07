using UnityEngine;

namespace Nova
{
    /// <summary>
    /// Create NovaController prefab
    /// </summary>
    public class NovaCreator : MonoBehaviour
    {
        public GameObject novaControllerPrefab;

        private void Awake()
        {
            var controllerCount = GameObject.FindGameObjectsWithTag("NovaController").Length;
            if (controllerCount > 1)
            {
                Debug.LogWarning("Nova: Multiple NovaController found in the scene.");
            }

            if (controllerCount >= 1)
            {
                return;
            }

            var controller = Instantiate(novaControllerPrefab);
            controller.tag = "NovaController";
            DontDestroyOnLoad(controller);
        }
    }
}
