using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Text))]
    public class TitleVersion : MonoBehaviour
    {
        private void Start()
        {
#if UNITY_EDITOR
            GetComponent<Text>().text = $"v{Application.version}";
#else
            GetComponent<Text>().text = $"v{Application.version} {Application.buildGUID.Substring(0, 8)}";
#endif
        }
    }
}
