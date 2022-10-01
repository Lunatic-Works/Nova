using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Text))]
    public class TitleVersion : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Text>().text = $"v{Application.version}";
        }
    }
}
