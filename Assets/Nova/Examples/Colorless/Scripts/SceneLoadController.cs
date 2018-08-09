using UnityEngine;
using UnityEngine.Events;

namespace Nova.Examples.Colorless.Scripts
{
    public class SceneLoadController : MonoBehaviour
    {
        public UnityAction DoAfterLoad;

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }
    }
}